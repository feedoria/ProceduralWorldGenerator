using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GenMenu
{
    const string GenScenePath = "Assets/Generator.unity";
    const string GenTerrainDataPath = "Assets/GenTerrainData.asset";

    static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
    static string ConfigDir => Path.Combine(ProjectRoot, "Configs");

    // run once from SampleScene, copies scene + terrain data so the pipeline never touches your terrain
    [MenuItem("Tools/Gen/Create Generator Scene")]
    static void CreateGeneratorScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GenScenePath) != null)
        {
            Debug.LogWarning("[Gen] Generator scene already exists");
            return;
        }

        Scene src = SceneManager.GetActiveScene();
        var ct = UnityEngine.Object.FindFirstObjectByType<CustomTerrain>();
        if (string.IsNullOrEmpty(src.path) || ct == null)
        {
            Debug.LogError("[Gen] open the saved scene with your CustomTerrain first");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string tdPath = AssetDatabase.GetAssetPath(ct.GetComponent<Terrain>().terrainData);
        AssetDatabase.CopyAsset(tdPath, GenTerrainDataPath);
        AssetDatabase.CopyAsset(src.path, GenScenePath);

        Scene scene = EditorSceneManager.OpenScene(GenScenePath);
        var gen = UnityEngine.Object.FindFirstObjectByType<CustomTerrain>();
        var td = AssetDatabase.LoadAssetAtPath<TerrainData>(GenTerrainDataPath);

        gen.gameObject.name = Gen.GenTerrainName;
        gen.GetComponent<Terrain>().terrainData = td;
        var col = gen.GetComponent<TerrainCollider>();
        if (col != null) col.terrainData = td;
        gen.terrain = gen.GetComponent<Terrain>();
        gen.terrainData = td;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Gen] Generator scene ready");
    }

    [MenuItem("Tools/Gen/Write Default Config")]
    static void WriteDefault()
    {
        Directory.CreateDirectory(ConfigDir);
        string path = Path.Combine(ConfigDir, "default.json");
        File.WriteAllText(path, new TerrainConfig().ToJson());
        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Tools/Gen/Save Inspector As Config")]
    static void SaveInspector()
    {
        var t = UnityEngine.Object.FindFirstObjectByType<CustomTerrain>();
        if (t == null) { Debug.LogError("[Gen] no CustomTerrain in scene"); return; }

        var c = new TerrainConfig
        {
            seed = UnityEngine.Random.Range(0, int.MaxValue),
            preset = "custom",
            voronoiPeaks = t.voronoiPeaks,
            voronoiFallOff = t.voronoiFallOff,
            voronoiDropOff = t.voronoiDropOff,
            voronoiMinHeight = t.voronoiMinHeight,
            voronoiMaxHeight = t.voronoiMaxHeight,
            voronoiType = t.voronoiType.ToString(),
            perlinLayers = t.perlinParameters.Select(p => new PerlinLayer
            {
                xScale = p.mPerlinXScale,
                yScale = p.mPerlinYScale,
                octaves = p.mPerlinOctaves,
                persistance = p.mPerlinPersistance,
                heightScale = p.mPerlinHeightScale
            }).ToList(),
            smoothIterations = t.smoothAmount
        };

        Directory.CreateDirectory(ConfigDir);
        string path = Path.Combine(ConfigDir, $"inspector_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        File.WriteAllText(path, c.ToJson());
        Debug.Log("[Gen] saved " + path);
        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Tools/Gen/Run Generator")]
    static void RunFromMenu()
    {
        if (SceneManager.GetActiveScene().path != GenScenePath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GenScenePath) == null)
            {
                Debug.LogError("[Gen] run Tools/Gen/Create Generator Scene first");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(GenScenePath);
        }

        string path = EditorUtility.OpenFilePanel("Config", ConfigDir, "json");
        if (string.IsNullOrEmpty(path)) return;

        var config = TerrainConfig.FromJson(File.ReadAllText(path));
        string outDir = Path.Combine(ProjectRoot, "runs", $"{DateTime.Now:yyyyMMdd_HHmmss}_s{config.seed}");
        int code = Gen.Run(config, outDir);
        Debug.Log($"[Gen] exit {code} -> {outDir}");
        EditorUtility.RevealInFinder(outDir);
    }

    // -executeMethod GenMenu.RunBatch -config cfg.json -out dir [-scene Assets/Generator.unity]
    public static void RunBatch()
    {
        string[] args = Environment.GetCommandLineArgs();
        string scene = Arg(args, "-scene") ?? GenScenePath;
        string cfg = Arg(args, "-config"), outDir = Arg(args, "-out");

        int code = Gen.Failed;
        if (cfg != null && outDir != null)
        {
            EditorSceneManager.OpenScene(scene);
            code = Gen.Run(TerrainConfig.FromJson(File.ReadAllText(cfg)), outDir);
        }
        EditorApplication.Exit(code);
    }

    static string Arg(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}