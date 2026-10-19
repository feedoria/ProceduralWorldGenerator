using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

// what goes into status.json, the site checks this file to know when a run is done
[Serializable]
public class RunStatus
{
    public string state; // "running" or "done"
    public int code;     // 0 ok, 1 failed, 2 rejected
    public string message;
    public string startedAt;
    public string finishedAt;
}

// the only entry point for a run, the editor menu and the exe both call Run() so they behave the same
public static class Gen
{
    public const int Ok = 0, Failed = 1, Rejected = 2;
    public const string GenTerrainName = "GenTerrain";

    public static int Run(TerrainConfig config, string outDir)
    {
        Directory.CreateDirectory(outDir);
        var ctx = new RunContext { config = config, outDir = outDir };
        var sw = Stopwatch.StartNew();
        var status = new RunStatus { state = "running", startedAt = DateTime.Now.ToString("o") };
        WriteStatus(outDir, status);

        int code = Failed;
        try
        {
            // only the GenTerrain object, so it never touches the terrain from my own scene
            ctx.terrain = FindGenTerrain();
            if (ctx.terrain == null)
                throw new Exception($"no {GenTerrainName} in scene, open the Generator scene");

            // OnEnable in CustomTerrain takes Terrain.activeTerrain, which can be the wrong terrain
            // so i set the terrain data from this exact object
            ctx.terrain.terrain = ctx.terrain.GetComponent<Terrain>();
            ctx.terrain.terrainData = ctx.terrain.terrain.terrainData;

            // a copy of the config next to the results, so every run knows exactly how it was made
            File.WriteAllText(Path.Combine(outDir, "config.json"), config.ToJson());

            // seeding both randoms -> same seed = same terrain
            UnityEngine.Random.InitState(config.seed);
            Utils.r = new System.Random(config.seed); // the teacher's Shuffle (used by River) has its own random
            ctx.Log($"seed {config.seed}, preset {config.preset}");

            foreach (var step in Pipeline.Build(config))
            {
                var t = Stopwatch.StartNew();
                step.Apply(ctx);
                ctx.Log($"{step.Name} done in {t.ElapsedMilliseconds} ms");
            }

            var m = RunOutput.Write(ctx, sw.Elapsed.TotalMilliseconds);
            ctx.Log($"hash {m.heightHash}");

            // basic quality check, if it's almost flat it's not a usable terrain
            if (m.maxHeight - m.minHeight < config.minHeightRange)
            {
                ctx.Log("rejected: too flat");
                status.message = "too flat";
                code = Rejected;
            }
            else
            {
                code = Ok;
            }
        }
        catch (Exception e)
        {
            // any error becomes code 1 instead of a crash, the details go in the log
            ctx.Log("ERROR " + e);
            status.message = e.Message;
            code = Failed;
        }
        finally
        {
            // the log first and status.json last, so when the site sees "done" everything else is already there
            File.WriteAllText(Path.Combine(outDir, "log.txt"), ctx.log.ToString());
            status.state = "done";
            status.code = code;
            status.finishedAt = DateTime.Now.ToString("o");
            WriteStatus(outDir, status);
        }
        return code;
    }

    static void WriteStatus(string outDir, RunStatus s)
    {
        // writing to a temp file and then swapping it, so the site never reads a half written file
        string path = Path.Combine(outDir, "status.json");
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonUtility.ToJson(s, true));
        if (File.Exists(path)) File.Replace(tmp, path, null);
        else File.Move(tmp, path);
    }

    static CustomTerrain FindGenTerrain()
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<CustomTerrain>(FindObjectsSortMode.None))
            if (t.gameObject.name == GenTerrainName) return t;
        return null;
    }
}