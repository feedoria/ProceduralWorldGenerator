using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class Gen
{
    public const int Ok = 0, Failed = 1, Rejected = 2;
    public const string GenTerrainName = "GenTerrain";

    public static int Run(TerrainConfig config, string outDir)
    {
        Directory.CreateDirectory(outDir);
        var ctx = new RunContext { config = config, outDir = outDir };
        var sw = Stopwatch.StartNew();
        try
        {
            ctx.terrain = FindGenTerrain();
            if (ctx.terrain == null)
                throw new Exception($"no {GenTerrainName} in scene, open the Generator scene");

            // her OnEnable uses activeTerrain, which can be the wrong one
            ctx.terrain.terrain = ctx.terrain.GetComponent<Terrain>();
            ctx.terrain.terrainData = ctx.terrain.terrain.terrainData;

            File.WriteAllText(Path.Combine(outDir, "config.json"), config.ToJson());
            UnityEngine.Random.InitState(config.seed);
            Utils.r = new System.Random(config.seed); // shuffle used by river
            ctx.Log($"seed {config.seed}, preset {config.preset}");

            foreach (var step in Pipeline.Build(config))
            {
                var t = Stopwatch.StartNew();
                step.Apply(ctx);
                ctx.Log($"{step.Name} done in {t.ElapsedMilliseconds} ms");
            }

            var m = RunOutput.Write(ctx, sw.Elapsed.TotalMilliseconds);
            ctx.Log($"hash {m.heightHash}");

            if (m.maxHeight - m.minHeight < config.minHeightRange)
            {
                ctx.Log("rejected: too flat");
                return Rejected;
            }
            return Ok;
        }
        catch (Exception e)
        {
            ctx.Log("ERROR " + e);
            return Failed;
        }
        finally
        {
            File.WriteAllText(Path.Combine(outDir, "log.txt"), ctx.log.ToString());
        }
    }

    static CustomTerrain FindGenTerrain()
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<CustomTerrain>(FindObjectsSortMode.None))
            if (t.gameObject.name == GenTerrainName) return t;
        return null;
    }
}