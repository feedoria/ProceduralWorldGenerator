using System;
using UnityEngine;

// sorted copy of the heights, used for percentiles (water level, sand, snow line, metrics)
public static class HeightStats
{
    public static float[] Sorted(float[,] heights)
    {
        float[] flat = new float[heights.Length];
        Buffer.BlockCopy(heights, 0, flat, 0, flat.Length * sizeof(float));
        Array.Sort(flat);
        return flat;
    }

    public static float[] Sorted(TerrainData td)
    {
        int res = td.heightmapResolution;
        return Sorted(td.GetHeights(0, 0, res, res));
    }

    // p between 0 and 1: 0.5 = median, 0.9 = 90% of the terrain is lower than this
    public static float Percentile(float[] sorted, float p) =>
        sorted[(int)(Mathf.Clamp01(p) * (sorted.Length - 1))];
}