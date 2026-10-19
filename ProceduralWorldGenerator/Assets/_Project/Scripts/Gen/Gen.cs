using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

[Serializable]
public class RunStatus
{
    public string state;        // running, done, failed, rejected
    public int code;
    public string message;
    public string startedAt;
    public string finishedAt;
}

// one generation run, same code for the editor menu and the .exe
// exit codes: 0 ok, 1 failed (crash), 2 rejected (config or result not good)
public static class Gen
{
    public const int Ok = 0, Failed = 1, Rejected = 2;
    public const string GenTerrainName = "GenTerrain";

    public static int Run(TerrainConfig config, string outDir)
    {
        Directory.CreateDirectory(outDir);
        var status = new RunStatus { state = "running", startedAt = DateTime.Now.ToString("o") };
        WriteStatus(outDir, status);

        var log = new StringBuilder();
        var total = Stopwatch.StartNew();
        int code;

        try
        {
            if (config.schemaVersion != TerrainConfig.CurrentSchema)
            {
                code = Rejected;
                status.message = $"config schemaVersion {config.schemaVersion}, expected {TerrainConfig.CurrentSchema}";
                log.AppendLine(status.message);
            }
            else
            {
                var ctx = new RunContext { terrain = FindGenTerrain(), config = config, outDir = outDir, log = log };
                code = RunPipeline(ctx, status, total);
            }
        }
        catch (Exception e)
        {
            code = Failed;
            status.message = e.Message;
            log.AppendLine(e.ToString());
            Debug.LogException(e);
        }
        finally
        {
            File.WriteAllText(Path.Combine(outDir, "log.txt"), log.ToString());
        }

        status.code = code;
        status.state = code == Ok ? "done" : code == Rejected ? "rejected" : "failed";
        status.finishedAt = DateTime.Now.ToString("o");
        WriteStatus(outDir, status);   // last file written, the site waits for it
        return code;
    }

    static int RunPipeline(RunContext ctx, RunStatus status, Stopwatch total)
    {
        var config = ctx.config;
        File.WriteAllText(Path.Combine(ctx.outDir, "config.json"), config.ToJson());
        ctx.Log($"seed {config.seed}, preset {config.preset}");

        // both randoms from the seed, same seed = same terrain
        UnityEngine.Random.InitState(config.seed);
        Utils.r = new System.Random(config.seed);   // Utils.Shuffle (used by River) has its own random

        foreach (var step in Pipeline.Steps())
        {
            if (!step.IsEnabled(config))
            {
                ctx.Log($"{step.Name}: off");
                continue;
            }
            var sw = Stopwatch.StartNew();
            step.Apply(ctx);
            ctx.Log($"{step.Name}: {sw.ElapsedMilliseconds} ms");
        }

        var metrics = RunOutput.Write(ctx);
        metrics.targetFails = Quality.Check(config, metrics);
        metrics.durationMs = total.ElapsedMilliseconds;
        RunOutput.WriteMetrics(ctx.outDir, metrics);

        if (metrics.targetFails.Count > 0)
        {
            status.message = string.Join("; ", metrics.targetFails);
            ctx.Log("rejected: " + status.message);
            return Rejected;
        }

        ctx.Log($"done in {metrics.durationMs} ms, hash {metrics.heightHash}");
        return Ok;
    }

    public static CustomTerrain FindGenTerrain()
    {
        var go = GameObject.Find(GenTerrainName);
        if (go == null) throw new Exception($"no '{GenTerrainName}' object in the open scene");

        var t = go.GetComponent<CustomTerrain>();
        if (t == null) throw new Exception($"'{GenTerrainName}' has no CustomTerrain component");

        // CustomTerrain's OnEnable uses activeTerrain, which can be the wrong terrain, so set both by hand
        t.terrain = go.GetComponent<Terrain>();
        t.terrainData = t.terrain.terrainData;
        return t;
    }

    // write to .tmp then swap, so the site never reads a half written file
    static void WriteStatus(string outDir, RunStatus s)
    {
        string path = Path.Combine(outDir, "status.json");
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonUtility.ToJson(s, true));
        if (File.Exists(path)) File.Replace(tmp, path, null);
        else File.Move(tmp, path);
    }
}
