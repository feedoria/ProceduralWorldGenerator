using System;
using System.Collections.Generic;
using UnityEngine;

// steps only copy config into the teacher's fields and call her methods

public class ResetStep : IStep
{
    public string Name => "Reset";
    public void Apply(RunContext ctx)
    {
        ctx.terrain.ResetTerrain();
        ctx.terrain.resetTerrain = false; // so the next steps stack
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
        t.voronoiType = StepUtil.ParseEnum(c.voronoiType, CustomTerrain.VoronoiType.Combined);

        // perlin voronoi reads the single perlin fields, so those come from config too
        PerlinLayer p = c.perlinLayers.Count > 0 ? c.perlinLayers[0] : new PerlinLayer();
        t.perlinXScale = p.xScale;
        t.perlinYScale = p.yScale;
        t.perlinOctaves = p.octaves;
        t.perlinPersistance = p.persistance;
        t.perlinHeightScale = p.heightScale;
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
    public static T ParseEnum<T>(string value, T fallback) where T : struct
    {
        return Enum.TryParse(value, true, out T result) ? result : fallback;
    }
}