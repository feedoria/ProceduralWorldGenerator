using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

// steps only copy config values into CustomTerrain's fields and call its methods
// Normalize is the only new algorithm, everything else already exists in CustomTerrain
// order is fixed in Pipeline.cs, a preset only turns steps on/off and picks values

public class ResetStep : IStep
{
    public string Name => "Reset";
    public bool IsEnabled(TerrainConfig c) => true;

    public void Apply(RunContext ctx)
    {
        var t = ctx.terrain;
        t.ResetTerrain();
        t.resetTerrain = false;                          // next steps add on top of what is already there
        ctx.Data.treeInstances = new TreeInstance[0];    // no leftover trees (they ended up in the water)
        StepUtil.DestroyByName("water");                 // leftovers from an older run in the same scene
        StepUtil.DestroyByName("ShoreLine");
    }
}

// mountain ridges: RidgeNoise folds the noise around 0.5 (h = 1 - |h - 0.5|)
// it resets the terrain by itself, that's why it has to be the first shape step
public class RidgeStep : IStep
{
    public string Name => "Ridge";
    public bool IsEnabled(TerrainConfig c) => c.ridge.enabled && c.ridge.layers.Count > 0;

    public void Apply(RunContext ctx)
    {
        var t = ctx.terrain;
        // heightScale forced to 1: the noise has to cover 0..1 so the fold at 0.5 makes ridges
        t.perlinParameters = StepUtil.ToPerlinParams(ctx.config.ridge.layers, 1f);
        t.RidgeNoise();

        // RidgeNoise gives heights between 0.5 and 1, nothing fits on top of that:
        // unity clamps heights at 1, so anything added later would turn into a flat plateau
        // so we bring the ridges down to 0..weight
        var td = ctx.Data;
        int res = td.heightmapResolution;
        float[,] h = td.GetHeights(0, 0, res, res);
        float w = ctx.config.ridge.weight;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
                h[y, x] = Mathf.Max(0f, (h[y, x] - 0.5f) * 2f * w);
        td.SetHeights(0, 0, h);
    }
}

// peaks: Voronoi keeps the max between the existing height and the cone, so peaks go on top of the ridges
public class VoronoiStep : IStep
{
    public string Name => "Voronoi";
    public bool IsEnabled(TerrainConfig c) => c.voronoi.enabled && c.voronoi.peaks > 0;

    public void Apply(RunContext ctx)
    {
        var t = ctx.terrain;
        var v = ctx.config.voronoi;

        t.voronoiPeaks = v.peaks;
        t.voronoiFallOff = v.fallOff;
        t.voronoiDropOff = v.dropOff;
        t.voronoiMinHeight = v.minHeight;
        t.voronoiMaxHeight = v.maxHeight;
        t.voronoiType = StepUtil.ParseEnum(v.type, CustomTerrain.VoronoiType.Combined);

        // the Perlin voronoi type reads the single perlin fields, so fill them from the first perlin layer
        var p = ctx.config.perlin.layers.Count > 0 ? ctx.config.perlin.layers[0] : new PerlinLayer();
        t.perlinXScale = p.xScale;
        t.perlinYScale = p.yScale;
        t.perlinOctaves = p.octaves;
        t.perlinPersistance = p.persistance;
        t.perlinHeightScale = p.heightScale;
        t.perlinOffsetX = Random.Range(0, 10000);
        t.perlinOffsetY = Random.Range(0, 10000);

        t.Voronoi();
    }
}

// main shapes (plains, hills) or just detail on top of the mountains
public class MultiplePerlinStep : IStep
{
    public string Name => "MultiplePerlin";
    public bool IsEnabled(TerrainConfig c) => c.perlin.enabled && c.perlin.layers.Count > 0;

    public void Apply(RunContext ctx)
    {
        var t = ctx.terrain;
        t.perlinParameters = StepUtil.ToPerlinParams(ctx.config.perlin.layers, null);
        t.MultiplePerlinTerrain();
    }
}

// the only new algorithm: stretch heights to 0..1 and give the terrain its real size
// after this, 0 = lowest point and 1 = reliefM meters, so erosion can work in meters and degrees
public class NormalizeStep : IStep
{
    public string Name => "Normalize";
    public bool IsEnabled(TerrainConfig c) => true;

    public void Apply(RunContext ctx)
    {
        var td = ctx.Data;
        int res = td.heightmapResolution;
        float[,] h = td.GetHeights(0, 0, res, res);

        float min = float.MaxValue, max = float.MinValue;
        int atCeiling = 0;
        foreach (float v in h)
        {
            if (v < min) min = v;
            if (v > max) max = v;
            if (v >= 0.9999f) atCeiling++;
        }
        // heights stuck at 1 = the shape layers add up to more than 1 and got cut flat
        float ceilingShare = (float)atCeiling / h.Length;
        if (ceilingShare > 0.01f)
            ctx.Log($"Normalize: WARNING {ceilingShare * 100:0.#}% of the terrain is cut at height 1, lower the layer weights");
        ctx.rawMin = min;
        ctx.rawMax = max;

        float range = max - min;
        if (range > 1e-6f)   // a flat terrain stays flat, Quality rejects it later
        {
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                    h[y, x] = (h[y, x] - min) / range;
            td.SetHeights(0, 0, h);
        }

        var c = ctx.config;
        td.size = new Vector3(c.terrainSizeM, c.reliefM, c.terrainSizeM);
        ctx.Log($"Normalize: raw range {min:0.0000}..{max:0.0000}, size {c.terrainSizeM} m, relief {c.reliefM} m");
    }
}

// slopes steeper than the talus angle slide down
// Thermal compares two neighbours: h > hn + erosionStrength
// so erosionStrength = the height difference of the talus angle over one cell, in 0..1 units
public class ThermalStep : IStep
{
    public string Name => "Thermal";
    public bool IsEnabled(TerrainConfig c) => c.thermal.enabled && c.thermal.passes > 0;

    public void Apply(RunContext ctx)
    {
        var t = ctx.terrain;
        var th = ctx.config.thermal;

        t.erosionType = CustomTerrain.ErosionType.Thermal;
        t.erosionStrength = Mathf.Tan(th.talusDeg * Mathf.Deg2Rad) * ctx.CellSizeM / ctx.config.reliefM;
        t.erosionAmount = th.amount;
        t.erosionSmoothAmount = 0;   // Erode smooths at the end, we have our own Smooth step

        for (int i = 0; i < th.passes; i++)
            t.Erode();

        ctx.Log($"Thermal: talus {th.talusDeg:0.#} deg -> strength {t.erosionStrength:0.00000}, {th.passes} passes");
    }
}

// rivers dig from a source downhill until their depth runs out
// River: depth at the source = erosionStrength, every step it loses solubility
// so length in cells = strength / solubility
public class RiverStep : IStep
{
    public string Name => "River";
    public bool IsEnabled(TerrainConfig c) => c.river.enabled && c.river.droplets > 0;

    public void Apply(RunContext ctx)
    {
        var t = ctx.terrain;
        var r = ctx.config.river;

        float strength = r.depthM / ctx.config.reliefM;
        float lengthCells = Mathf.Max(1f, r.lengthM / ctx.CellSizeM);

        t.erosionType = CustomTerrain.ErosionType.River;
        t.erosionStrength = strength;
        t.solubility = strength / lengthCells;
        t.droplets = r.droplets;
        t.springsPerRiver = r.springs;
        t.erosionSmoothAmount = 0;

        t.Erode();
        ctx.Log($"River: {r.droplets} sources x {r.springs}, depth {r.depthM} m, ~{lengthCells:0} cells long");
    }
}

public class SmoothStep : IStep
{
    public string Name => "Smooth";
    public bool IsEnabled(TerrainConfig c) => c.smooth.enabled && c.smooth.iterations > 0;

    public void Apply(RunContext ctx)
    {
        var t = ctx.terrain;
        t.smoothAmount = ctx.config.smooth.iterations;
        t.Smooth();
    }
}

// water level = a percentile of the heights, so "5% under water" means the same on any terrain
public class WaterStep : IStep
{
    public string Name => "Water";
    public bool IsEnabled(TerrainConfig c) => c.water.enabled;

    public void Apply(RunContext ctx)
    {
        var t = ctx.terrain;
        float p = ctx.config.water.percentile;

        ctx.waterPercentile = p;
        ctx.waterHeight = HeightStats.Percentile(ctx.SortedHeights(), p);
        t.waterHeight = ctx.waterHeight;

        if (t.waterGO == null && GameObject.Find("water") == null)
        {
            ctx.Log("Water: no Water GO set on GenTerrain, level computed but no water plane");
            return;
        }
        t.AddWater();
        ctx.Log($"Water: P{p * 100:0.#} -> {ctx.waterHeight * ctx.Data.size.y:0.0} m");
    }
}

// 4 roles, in this order in the splat table on GenTerrain: sand, grass, rock, snow
// thresholds are relative (water level, percentiles, slope), so they fit any relief
public class TextureStep : IStep
{
    public string Name => "Textures";
    public bool IsEnabled(TerrainConfig c) => c.textures.enabled;

    public void Apply(RunContext ctx)
    {
        var t = ctx.terrain;
        var tx = ctx.config.textures;

        if (t.splatHeights.Count < 4)
        {
            ctx.Log("Textures: the splat table needs 4 rows (sand, grass, rock, snow), skipped");
            return;
        }
        for (int i = 0; i < 4; i++)
        {
            if (t.splatHeights[i].texture == null)
            {
                ctx.Log($"Textures: row {i} has no texture, skipped");
                return;
            }
        }

        float[] sorted = ctx.SortedHeights();
        float sandTop = HeightStats.Percentile(sorted, ctx.waterPercentile + tx.sandBand);
        float snowLine = tx.snowEnabled ? HeightStats.Percentile(sorted, tx.snowPercentile) : 2f;  // 2 = never
        float rock = tx.rockSlopeDeg;

        Set(t.splatHeights[0], 0f, sandTop, 0f, rock);                      // sand: by the water, not steep
        Set(t.splatHeights[1], sandTop, Mathf.Min(snowLine, 1f), 0f, rock); // grass: everything in between
        Set(t.splatHeights[2], 0f, 1f, rock, 90f);                          // rock: any height, steep
        Set(t.splatHeights[3], snowLine, 1f, 0f, 90f);                      // snow: the top
        for (int i = 4; i < t.splatHeights.Count; i++)
            Set(t.splatHeights[i], 2f, 2f, 90f, 90f);                       // extra rows never show

        t.SplatMaps();
        ctx.Log($"Textures: sand under {sandTop:0.000}, rock over {rock:0.#} deg, snow {(tx.snowEnabled ? "over " + snowLine.ToString("0.000") : "off")}");
    }

    // every pixel always matches at least one row (sand/grass cover low slopes, rock the steep ones),
    // otherwise the weights would be all 0 and normalizing them would break
    static void Set(CustomTerrain.SplatHeights s, float minH, float maxH, float minSlope, float maxSlope)
    {
        s.minHeight = minH;
        s.maxHeight = maxH;
        s.minSlope = minSlope;
        s.maxSlope = maxSlope;
        s.splatOffset = 0.01f;        // soft edges between the bands
        s.splatNoiseXScale = 0.01f;
        s.splatNoiseYScale = 0.01f;
        s.splatNoiseScaler = 0.02f;
    }
}

public class ShorelineStep : IStep
{
    public string Name => "Shoreline";
    public bool IsEnabled(TerrainConfig c) => c.shoreline.enabled && c.water.enabled;

    public void Apply(RunContext ctx)
    {
        var t = ctx.terrain;
        if (t.shoreLineMaterial == null)
        {
            ctx.Log("Shoreline: no Shore Line Material set on GenTerrain, skipped");
            return;
        }
        t.DrawShoreline();
    }
}

public static class StepUtil
{
    public static T ParseEnum<T>(string value, T fallback) where T : struct =>
        Enum.TryParse(value, true, out T result) ? result : fallback;

    // config layers -> CustomTerrain's list, every layer gets random offsets (seeded, so still deterministic)
    public static List<CustomTerrain.PerlinParameters> ToPerlinParams(List<PerlinLayer> layers, float? forceHeightScale)
    {
        var list = new List<CustomTerrain.PerlinParameters>();
        foreach (var l in layers)
        {
            list.Add(new CustomTerrain.PerlinParameters
            {
                mPerlinXScale = l.xScale,
                mPerlinYScale = l.yScale,
                mPerlinOctaves = l.octaves,
                mPerlinPersistance = l.persistance,
                mPerlinHeightScale = forceHeightScale ?? l.heightScale,
                mPerlinOffsetX = Random.Range(0, 10000),
                mPerlinOffsetY = Random.Range(0, 10000)
            });
        }
        return list;
    }

    public static void DestroyByName(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }
}