using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

// everything that goes into metrics.json, the site reads exactly these fields
[Serializable]
public class RunMetrics
{
    public int schemaVersion = 1;
    public int seed;
    public string preset;
    public int resolution;
    public string layout = "row-major [y,x], float32"; // so whoever reads heightmap.raw knows the order
    public float minHeight;
    public float maxHeight;
    public float meanHeight;
    public float heightStdDev; // how varied the terrain is
    // percentiles: 10% of the terrain is lower than P10, half is lower than P50 etc
    // P10 is gonna be the base for the water level later
    public float heightP10;
    public float heightP50;
    public float heightP90;
    public float meanSlopeDeg;
    // % of the terrain in each slope band: 0-5, 5-15, 15-30, 30-45, 45+ degrees
    // easy to compare with a real DEM later
    public float[] slopeBands = new float[5];
    public string heightHash; // same hash = exactly the same terrain
    public double durationMs;
}

public static class RunOutput
{
    static readonly float[] SlopeLimits = { 5f, 15f, 30f, 45f };

    public static RunMetrics Write(RunContext ctx, double durationMs)
    {
        TerrainData td = ctx.Data;
        int res = td.heightmapResolution;
        int n = res * res;
        float[,] h = td.GetHeights(0, 0, res, res);

        // raw float32 and not png because a png only has 256 grey levels, the metrics need the real values
        // BlockCopy copies the whole float matrix into bytes in one go
        byte[] raw = new byte[n * sizeof(float)];
        Buffer.BlockCopy(h, 0, raw, 0, raw.Length);
        File.WriteAllBytes(Path.Combine(ctx.outDir, "heightmap.raw"), raw);

        var m = new RunMetrics
        {
            seed = ctx.config.seed,
            preset = ctx.config.preset,
            resolution = res,
            durationMs = durationMs
        };

        // going through the heights and the slopes in one single pass
        float[] flat = new float[n]; // copy of the heights so i can sort it for the percentiles
        float min = float.MaxValue, max = float.MinValue;
        double sum = 0, sumSq = 0, slopeSum = 0;
        int[] bandCount = new int[5];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float v = h[y, x];
                flat[y * res + x] = v;
                if (v < min) min = v;
                if (v > max) max = v;
                sum += v;
                sumSq += v * v;

                // unity gives the slope in degrees, coords are 0-1
                float s = td.GetSteepness((float)x / (res - 1), (float)y / (res - 1));
                slopeSum += s;
                // finding which band the slope falls in
                int b = 0;
                while (b < SlopeLimits.Length && s >= SlopeLimits[b]) b++;
                bandCount[b]++;
            }

        double mean = sum / n;
        m.minHeight = min;
        m.maxHeight = max;
        m.meanHeight = (float)mean;
        // std dev = sqrt(mean of squares - square of mean)
        m.heightStdDev = (float)Math.Sqrt(Math.Max(0, sumSq / n - mean * mean));
        m.meanSlopeDeg = (float)(slopeSum / n);
        for (int b = 0; b < 5; b++)
            m.slopeBands[b] = 100f * bandCount[b] / n;

        // after sorting, the value at 10% of the list is P10 and so on
        Array.Sort(flat);
        m.heightP10 = flat[(int)(0.1 * (n - 1))];
        m.heightP50 = flat[(int)(0.5 * (n - 1))];
        m.heightP90 = flat[(int)(0.9 * (n - 1))];

        // grey preview, stretched from min to max otherwise it's almost black (heights are usually 0.1-0.3)
        float range = Mathf.Max(max - min, 1e-6f);
        var px = new Color32[n];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                byte g = (byte)(255 * (h[y, x] - min) / range);
                px[y * res + x] = new Color32(g, g, g, 255);
            }
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(ctx.outDir, "preview.png"), tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        RunCameras.Capture(ctx);
        ctx.Log("screenshots done");

        // sha256 of the heightmap bytes, i only keep the first 16 characters, it's enough to compare
        using (var sha = SHA256.Create())
            m.heightHash = BitConverter.ToString(sha.ComputeHash(raw)).Replace("-", "").Substring(0, 16);

        File.WriteAllText(Path.Combine(ctx.outDir, "metrics.json"), JsonUtility.ToJson(m, true));
        return m;
    }
}