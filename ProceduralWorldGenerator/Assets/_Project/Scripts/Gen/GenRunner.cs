using System;
using System.Collections;
using System.IO;
using UnityEngine;

// what the .exe does when it starts, reads the command line:
//   -config cfg.json -out dir   generate, write the files, close (what the site's worker uses)
//   -config cfg.json -view      generate and stay open with a free camera (the 3D view)
// in the editor it does nothing, there you use Tools/Gen
public class GenRunner : MonoBehaviour
{
    IEnumerator Start()
    {
        Application.runInBackground = true;   // keep working even if the window isn't focused
        if (Application.isEditor) yield break;

        yield return null;   // one frame, so the scene is fully loaded

        string configPath = Arg("-config");
        if (configPath == null) yield break;   // started by hand, nothing to do

        TerrainConfig config;
        try
        {
            config = TerrainConfig.FromJson(File.ReadAllText(configPath));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Application.Quit(Gen.Failed);
            yield break;
        }

        if (HasFlag("-view"))
        {
            try
            {
                var t = Gen.Preview(config);
                FlyCamera.SetUp(t.GetComponent<Terrain>(), $"{config.preset} · seed {config.seed}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Application.Quit(Gen.Failed);
            }
            yield break;   // stays open until Esc or the window is closed
        }

        string outDir = Arg("-out");
        if (outDir == null)
        {
            Debug.LogError("[Gen] -config without -out or -view");
            Application.Quit(Gen.Failed);
            yield break;
        }

        int code = Gen.Run(config, Path.GetFullPath(outDir));
        Application.Quit(code);
    }

    static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    static bool HasFlag(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
}