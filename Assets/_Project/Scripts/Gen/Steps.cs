using System;
using System.Collections.Generic;
using UnityEngine;

// every step just copies the values from the config into the teacher's fields
// and then calls her method, so her code stays untouched

public class ResetStep : IStep
{
    public string Name => "Reset";
    public void Apply(RunContext ctx)
    {
        ctx.terrain.ResetTerrain();
        // after the reset the next steps have to add on top of each other, not start from zero again
        ctx.terrain.resetTerrain = false;
        // the generator scene was copied from my scene so it still had my old trees on it
        ctx.Data.treeInstances = new TreeInstance[0];
    }
}

public class VoronoiStep : IStep
{
    public string Name => "Voronoi";
    public void Apply(RunContext ctx)
    {
        var t = ctx.terrain;
        var c = ctx.config;
        t.voronoiPeaks = c.voronoiPeaks;
        t.voronoiFallOff = c.voronoiFallOff;
        t.voronoiDropOff = c.voronoiDropOff;
        t.voronoiMinHeight = c.voronoiMinHeight;
        t.voronoiMaxHeight = c.voronoiMaxHeight;
        // the type is a string in the json so it's readable, if it's misspelled it falls back to Combined
        t.voronoiType = StepUtil.ParseEnum(c.voronoiType, CustomTerrain.VoronoiType.Combined);

        // the Perlin type of voronoi reads the single perlin fields, so i set those from the config too
        // otherwise the result would depend on whatever was left in the inspector
        PerlinLayer p = c.perlinLayers.Count > 0 ? c.perlinLayers[0] : new PerlinLayer();
        t.perlinXScale = p.xScale;
        t.perlinYScale = p.yScale;
        t.perlinOctaves = p.octaves;
        t.perlinPersistance = p.persistance;
        t.perlinHeightScale = p.heightScale;
        // offsets come from the seed (Random is already seeded in Gen.Run)
        t.perlinOffsetX = UnityEngine.Random.Range(0, 10000);
        t.perlinOffsetY = UnityEngine.Random.Range(0, 10000);

        t.Voronoi();
    }
}

public class MultiplePerlinStep : IStep
{
    public string Name => "Multiple Perlin";
    public void Apply(RunContext ctx)
    {
        // turning every layer from the config into a row of the teacher's perlin table
        var list = new List<CustomTerrain.PerlinParameters>();
        foreach (PerlinLayer p in ctx.config.perlinLayers)
        {
            list.Add(new CustomTerrain.PerlinParameters
            {
                mPerlinXScale = p.xScale,
                mPerlinYScale = p.yScale,
                mPerlinOctaves = p.octaves,
                mPerlinPersistance = p.persistance,
                mPerlinHeightScale = p.heightScale,
                mPerlinOffsetX = UnityEngine.Random.Range(0, 10000),
                mPerlinOffsetY = UnityEngine.Random.Range(0, 10000)
            });
        }
        if (list.Count == 0) return;
        ctx.terrain.perlinParameters = list;
        ctx.terrain.MultiplePerlinTerrain();
    }
}

public class SmoothStep : IStep
{
    public string Name => "Smooth";
    public void Apply(RunContext ctx)
    {
        if (ctx.config.smoothIterations <= 0) return;
        ctx.terrain.smoothAmount = ctx.config.smoothIterations;
        ctx.terrain.Smooth();
    }
}

public static class StepUtil
{
    // string -> enum, ignoring upper/lower case, with a fallback if the name doesnt exist
    public static T ParseEnum<T>(string value, T fallback) where T : struct
    {
        return Enum.TryParse(value, true, out T result) ? result : fallback;
    }
}