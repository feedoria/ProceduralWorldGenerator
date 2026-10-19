using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

[Serializable]
public class RunMetrics
{
    public int schemaVersion = 1;
    public int seed;
    public string preset;
    public int resolution;
    public string layout = "row-major [y,x], float32";
    public float minHeight;
    public float maxHeight;
    public float meanHeight;
    public float meanSlopeDeg;
    public string heightHash;
    public double durationMs;
}

public static class RunOutput
{
    public static RunMetrics Write(RunContext ctx, double durationMs)
    {
        TerrainData td = ctx.Data;
        int res = td.heightmapResolution;
        float[,] h = td.GetHeights(0, 0, res, res);

        byte[] raw = new byte[res * res * sizeof(float)];
        Buffer.BlockCopy(h, 0, raw, 0, raw.Length);
        File.WriteAllBytes(Path.Combine(ctx.outDir, "heightmap.raw"), raw);

        float min = float.MaxValue, max = float.MinValue;
        double sum = 0, slopeSum = 0;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float v = h[y, x];
                if (v < min) min = v;
                if (v > max) max = v;
                sum += v;
                slopeSum += td.GetSteepness((float)x / (res - 1), (float)y / (res - 1));
            }

        // stretched to min-max so it's actually visible
        float range = Mathf.Max(max - min, 1e-6f);
        var px = new Color32[res * res];
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

        var m = new RunMetrics
        {
            seed = ctx.config.seed,
            preset = ctx.config.preset,
            resolution = res,
            minHeight = min,
            maxHeight = max,
            meanHeight = (float)(sum / (res * res)),
            meanSlopeDeg = (float)(slopeSum / (res * res)),
            durationMs = durationMs
        };
        using (var sha = SHA256.Create())
            m.heightHash = BitConverter.ToString(sha.ComputeHash(raw)).Replace("-", "").Substring(0, 16);

        File.WriteAllText(Path.Combine(ctx.outDir, "metrics.json"), JsonUtility.ToJson(m, true));
        return m;
    }
}