using System;
using System.Collections;
using System.IO;
using UnityEngine;

// the entry point of the exe, in the editor i use Tools/Gen instead
// one-shot mode for now: Generator.exe -config cfg.json -out folder
// it generates, writes the files and closes with the exit code (0 ok, 1 failed, 2 rejected)
public class GenRunner : MonoBehaviour
{
    IEnumerator Start()
    {
        // otherwise the exe freezes when i click on another window (like the browser)
        Application.runInBackground = true;

        if (Application.isEditor) yield break; // play mode in the editor shouldnt start anything

        // waiting one frame so the terrain and the render pipeline are fully set up before i use them
        yield return null;

        string cfg = Arg("-config");
        string outDir = Arg("-out");

        if (cfg == null && outDir == null)
        {
            // no arguments = nothing to do yet, later this is gonna be the viewer mode that waits for jobs
            Debug.Log("[Gen] no -config / -out, idle");
            yield break;
        }

        int code = Gen.Failed;
        if (cfg != null && outDir != null && File.Exists(cfg))
        {
            var config = TerrainConfig.FromJson(File.ReadAllText(cfg));
            code = Gen.Run(config, Path.GetFullPath(outDir));
        }
        else
        {
            Debug.LogError("[Gen] need both -config (existing file) and -out");
        }

        Application.Quit(code);
    }

    // finds the value after a name in the command line, ex: -config C:\x.json -> C:\x.json
    static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}