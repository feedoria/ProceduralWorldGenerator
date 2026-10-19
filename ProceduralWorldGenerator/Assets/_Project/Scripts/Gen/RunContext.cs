using System;
using System.Text;
using UnityEngine;

// everything one run needs, passed from step to step
public class RunContext
{
    public CustomTerrain terrain;
    public TerrainConfig config;
    public string outDir;
    public StringBuilder log = new StringBuilder();

    // filled by the steps while the pipeline runs
    public float rawMin, rawMax;        // heights before Normalize, to catch flat terrains
    public float waterHeight;           // 0..1, stays 0 when there is no water
    public float waterPercentile;       // the percentile used for the water, textures start from it

    float[] sorted;

    public TerrainData Data => terrain.terrainData;
    public int Res => Data.heightmapResolution;
    public float CellSizeM => config.terrainSizeM / (Res - 1);   // meters between two heightmap points

    // sorted heights for percentiles, computed once
    // only call it after the steps that change heights (water and later)
    public float[] SortedHeights()
    {
        if (sorted == null) sorted = HeightStats.Sorted(Data);
        return sorted;
    }

    public void Log(string msg)
    {
        log.AppendLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
        Debug.Log("[Gen] " + msg);
    }
}