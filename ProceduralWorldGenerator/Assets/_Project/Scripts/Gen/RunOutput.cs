using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class RunMetrics
{
    public int schemaVersion = TerrainConfig.CurrentSchema;
    public int seed;
    public string preset;
    public int resolution;
    public string layout = "heightmap.raw: float32, row-major [y,x], 0..1 (x reliefM = meters)";

    public float terrainSizeM;
    public float reliefM;

    // heights are 0..1, multiply by reliefM for meters
    public float minHeight, maxHeight, meanHeight, heightStdDev;
    public float heightP10, heightP50, heightP90;
    public float rawHeightRange;        // before Normalize

    public float waterHeight;           // 0..1, 0 = no water
    public float waterLevelM;

    public float meanSlopeDeg;
    public float[] slopeBands = new float[5];   // share of terrain: 0-5, 5-15, 15-30, 30-45, 45+ degrees

    public string heightHash;           // first 16 hex chars of SHA-256(heightmap.raw)
    public long durationMs;

    public bool targetsChecked;
    public List<string> targetFails = new List<string>();
}

// writes everything a run leaves behind, except status.json (Gen writes that one last)
public static class RunOutput
{
    public static RunMetrics Write(RunContext ctx)
    {
        var td = ctx.Data;
        int res = td.heightmapResolution;
        float[,] h = td.GetHeights(0, 0, res, res);

        // raw heightmap, same bytes as in memory
        byte[] raw = new byte[h.Length * sizeof(float)];
        Buffer.BlockCopy(h, 0, raw, 0, raw.Length);
        File.WriteAllBytes(Path.Combine(ctx.outDir, "heightmap.raw"), raw);

        var m = new RunMetrics
        {
            seed = ctx.config.seed,
            preset = ctx.config.preset,
            resolution = res,
            terrainSizeM = td.size.x,
            reliefM = td.size.y,
            rawHeightRange = ctx.rawMax - ctx.rawMin,
            waterHeight = ctx.waterHeight,
            waterLevelM = ctx.waterHeight * td.size.y
        };

        // one pass: min, max, sums for mean and std dev, slope
        float min = float.MaxValue, max = float.MinValue;
        double sum = 0, sumSq = 0, slopeSum = 0;
        int[] bands = new int[5];
        float inv = 1f / (res - 1);

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float v = h[y, x];
                if (v < min) min = v;
                if (v > max) max = v;
                sum += v;
                sumSq += v * v;

                float s = td.GetSteepness(x * inv, y * inv);   // degrees, uses the real size
                slopeSum += s;
                bands[Band(s)]++;
            }
        }

        int n = h.Length;
        double mean = sum / n;
        m.minHeight = min;
        m.maxHeight = max;
        m.meanHeight = (float)mean;
        m.heightStdDev = (float)Math.Sqrt(Math.Max(0, sumSq / n - mean * mean));   // E[X^2] - E[X]^2
        m.meanSlopeDeg = (float)(slopeSum / n);
        for (int i = 0; i < 5; i++) m.slopeBands[i] = (float)bands[i] / n;

        float[] sorted = HeightStats.Sorted(h);
        m.heightP10 = HeightStats.Percentile(sorted, 0.1f);
        m.heightP50 = HeightStats.Percentile(sorted, 0.5f);
        m.heightP90 = HeightStats.Percentile(sorted, 0.9f);

        WritePreview(h, res, min, max, Path.Combine(ctx.outDir, "preview.png"));
        RunCameras.Capture(ctx.terrain.GetComponent<Terrain>(), ctx.outDir);

        using (var sha = SHA256.Create())
            m.heightHash = BitConverter.ToString(sha.ComputeHash(raw)).Replace("-", "").Substring(0, 16);

        return m;
    }

    public static void WriteMetrics(string outDir, RunMetrics m) =>
        File.WriteAllText(Path.Combine(outDir, "metrics.json"), JsonUtility.ToJson(m, true));

    static int Band(float s) => s < 5f ? 0 : s < 15f ? 1 : s < 30f ? 2 : s < 45f ? 3 : 4;

    // grayscale heightmap, stretched so the lowest point is black and the highest white
    static void WritePreview(float[,] h, int res, float min, float max, string path)
    {
        float range = Mathf.Max(max - min, 1e-6f);
        var pixels = new Color32[res * res];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                byte g = (byte)(255f * (h[y, x] - min) / range);
                pixels[y * res + x] = new Color32(g, g, g, 255);
            }

        var tex = new Texture2D(res, res, TextureFormat.RGB24, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}
