using UnityEngine;
using UnityEditor;
using EditorGUITable; // the asset guitable that i have in the project

[CustomEditor(typeof(CustomTerrain))] // CustomTerrain and CustomTerrainEditor become linked together so we can call info between them
[CanEditMultipleObjects]

public class CustomTerrainEditor : Editor
{

    //properties -----------
    SerializedProperty randomHeightRange; // float in this case -> linking this with the other one in the other script
    SerializedProperty heightMapScale;
    SerializedProperty heightMapImage;
    SerializedProperty perlinXScale;
    SerializedProperty perlinYScale;
    SerializedProperty perlinOffsetX;
    SerializedProperty perlinOffsetY;
    SerializedProperty perlinOctaves;
    SerializedProperty perlinPersistance;
    SerializedProperty perlinHeightScale;
    SerializedProperty resetTerrain;
    SerializedProperty voronoiFallOff;
    SerializedProperty voronoiDropOff;
    SerializedProperty voronoiMinHeight;
    SerializedProperty voronoiMaxHeight;
    SerializedProperty voronoiPeaks;
    SerializedProperty voronoiType;
    SerializedProperty MPDheightMin;
    SerializedProperty MPDheightMax;
    SerializedProperty MPDheightDampenerPower;
    SerializedProperty MPDroughness;

    SerializedProperty smoothAmount;

    GUITableState splatMapTable;
    SerializedProperty splatHeights;
    //SerializedProperty splatOffset;
    //SerializedProperty splatNoiseXScale;
    //SerializedProperty splatNoiseYScale;
    //SerializedProperty splatNoiseScaler;

    GUITableState perlinParameterTable;
    SerializedProperty perlinParameters;

    GUITableState vegMapTable;
    SerializedProperty vegetation;
    SerializedProperty maxTrees;
    SerializedProperty treeSpacing;


    //fold outs ------------
    bool showRandom = false;
    bool showLoadHeights = false;
    bool showPerlinNoise = false;
    bool showMultiplePerlin = false;
    bool showVoronoi = false;
    bool showMPD = false;
    bool showSmooth = false;
    bool showSplatMaps = false;
    bool showVeg = false;
    bool showHeights = false;

    Texture2D hmTexture;

    // that s gonna run every time i enable the terrain
    void OnEnable()
    {
        // the linking code
        randomHeightRange = serializedObject.FindProperty("randomHeightRange");
        // so when we change in the inspector now we know that the values are gonna remain there
        heightMapScale = serializedObject.FindProperty("heightMapScale");
        heightMapImage = serializedObject.FindProperty("heightMapImage");
        perlinXScale = serializedObject.FindProperty("perlinXScale");
        perlinYScale = serializedObject.FindProperty("perlinYScale");
        perlinOffsetX = serializedObject.FindProperty("perlinOffsetX");
        perlinOffsetY = serializedObject.FindProperty("perlinOffsetY");
        perlinOctaves = serializedObject.FindProperty("perlinOctaves");
        perlinPersistance = serializedObject.FindProperty("perlinPersistance");
        perlinHeightScale = serializedObject.FindProperty("perlinHeightScale");
        resetTerrain = serializedObject.FindProperty("resetTerrain");
        perlinParameterTable = new GUITableState("perlinParameterTable");
        perlinParameters = serializedObject.FindProperty("perlinParameters");
        voronoiDropOff = serializedObject.FindProperty("voronoiDropOff");
        voronoiFallOff = serializedObject.FindProperty("voronoiFallOff");
        voronoiMinHeight = serializedObject.FindProperty("voronoiMinHeight");
        voronoiMaxHeight = serializedObject.FindProperty("voronoiMaxHeight");
        voronoiPeaks = serializedObject.FindProperty("voronoiPeaks");
        voronoiType = serializedObject.FindProperty("voronoiType");
        MPDheightMin = serializedObject.FindProperty("MPDheightMin");
        MPDheightMax = serializedObject.FindProperty("MPDheightMax");
        MPDheightDampenerPower = serializedObject.FindProperty("MPDheightDampenerPower");
        MPDroughness = serializedObject.FindProperty("MPDroughness");
        smoothAmount = serializedObject.FindProperty("smoothAmount");
        splatMapTable = new GUITableState("splatMapTable");
        splatHeights = serializedObject.FindProperty("splatHeights");
        //splatOffset = serializedObject.FindProperty("splatOffset");
        //splatNoiseXScale = serializedObject.FindProperty("splatNoiseXScale");
        //splatNoiseYScale = serializedObject.FindProperty("splatNoiseYScale");
        //splatNoiseScaler = serializedObject.FindProperty("splatNoiseScaler");
        vegMapTable = new GUITableState("vegMapTable");
        vegetation = serializedObject.FindProperty("vegetation");
        maxTrees = serializedObject.FindProperty("maxTrees");
        treeSpacing = serializedObject.FindProperty("treeSpacing");

        hmTexture = new Texture2D(513, 513, TextureFormat.ARGB32, false);
    }
    Vector2 scrollPos;
    public override void OnInspectorGUI() // this makes the this in inspector appear buttons sliders whatever
    {
        // always at the beginning
        serializedObject.Update(); // it updates all the serialized values between this script and the ser from customTerrain script

        //changing the values
        // we can access the randomHights field from customTerrain
        CustomTerrain terrain = (CustomTerrain) target;
        // the thing is called the target is the script/class that is liked to 
        // terrain is a link to the script not to the actual terrain itself
        // I want to use serialized values so i csn change things from inspector 
        // so I'm not gonna set the terrain.random.... directly

        //Scrollbar Starting Code
        Rect r = EditorGUILayout.BeginVertical();
        scrollPos =
            EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(r.width), GUILayout.Height(r.height));
        EditorGUI.indentLevel++;


        EditorGUILayout.PropertyField(resetTerrain);
        showRandom = EditorGUILayout.Foldout(showRandom, "Random"); // foldout is the arrow that hides the details 
        if (showRandom)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Set Heights Between Random Values", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(randomHeightRange);
            if (GUILayout.Button("Random Heights"))
            {
                terrain.RandomTerrain();
            }
        }

        showLoadHeights = EditorGUILayout.Foldout(showLoadHeights, "Load Heights");
        if (showLoadHeights)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Load Heights From Texture", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(heightMapImage);
            EditorGUILayout.PropertyField(heightMapScale);
            if (GUILayout.Button("Load Texture"))
            {
                terrain.LoadTexture();
            }
        }

        showPerlinNoise = EditorGUILayout.Foldout(showPerlinNoise, "Single Perlin Noise");
        if (showPerlinNoise)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Perlin Noise", EditorStyles.boldLabel);
            EditorGUILayout.Slider(perlinXScale, 0, 1, new GUIContent("X Scale"));
            EditorGUILayout.Slider(perlinYScale, 0, 1, new GUIContent("Y Scale"));
            EditorGUILayout.IntSlider(perlinOffsetX, 0, 10000, new GUIContent("Offset X"));
            EditorGUILayout.IntSlider(perlinOffsetY, 0, 10000, new GUIContent("Offset Y"));
            EditorGUILayout.IntSlider(perlinOctaves, 1, 10, new GUIContent("Octaves"));
            EditorGUILayout.Slider(perlinPersistance, 0.1f, 10, new GUIContent("Persistance"));
            EditorGUILayout.Slider(perlinHeightScale, 0, 1, new GUIContent("Height Scale"));

            if (GUILayout.Button("Perlin"))
            {
                terrain.Perlin();
            }
        }

        showMultiplePerlin = EditorGUILayout.Foldout(showMultiplePerlin, "Multiple Perlin Noise");
        if (showMultiplePerlin)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Mulitple Perlin Noise", EditorStyles.boldLabel);
            perlinParameterTable = GUITableLayout.DrawTable(perlinParameterTable,
                                        serializedObject.FindProperty("perlinParameters"));
            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewPerlin();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemovePerlin();
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Apply Multiple Perlin"))
            {
                terrain.MultiplePerlinTerrain();
            }

            // de la mine
            GUILayout.Space(20);
            if (GUILayout.Button("Apply Ridge Noise"))
            {
                terrain.RidgeNoise();
            }
        }

        showVoronoi = EditorGUILayout.Foldout(showVoronoi, "Voronoi");
        if (showVoronoi)
        {
            EditorGUILayout.IntSlider(voronoiPeaks, 1, 10, new GUIContent("Peak Count"));
            EditorGUILayout.Slider(voronoiFallOff, 0, 10, new GUIContent("Falloff"));
            EditorGUILayout.Slider(voronoiDropOff, 0, 10, new GUIContent("Dropoff"));
            EditorGUILayout.Slider(voronoiMinHeight, 0, 1, new GUIContent("Min Height"));
            EditorGUILayout.Slider(voronoiMaxHeight, 0, 1, new GUIContent("Max Height"));
            EditorGUILayout.PropertyField(voronoiType);
            if (GUILayout.Button("Voronoi"))
            {
                terrain.Voronoi(); 
            }
        }

        showMPD = EditorGUILayout.Foldout(showMPD, "Midpoint Displacement");
        if (showMPD)
        {
            EditorGUILayout.PropertyField(MPDheightMin);
            EditorGUILayout.PropertyField(MPDheightMax);
            EditorGUILayout.PropertyField(MPDheightDampenerPower);
            EditorGUILayout.PropertyField(MPDroughness);
            if (GUILayout.Button("MPD"))
            {
                terrain.MidPointDisplacement();
            }
        }

        showSplatMaps = EditorGUILayout.Foldout(showSplatMaps, "Splat Maps");
        if (showSplatMaps)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Splat Maps", EditorStyles.boldLabel);
            /*EditorGUILayout.Slider(splatOffset, 0, 0.1f, new GUIContent("Offset"));
            EditorGUILayout.Slider(splatNoiseXScale, 0.001f, 1, new GUIContent("Noise X Scale"));
            EditorGUILayout.Slider(splatNoiseYScale, 0.001f, 1, new GUIContent("Noise Y Scale"));
            EditorGUILayout.Slider(splatNoiseScaler, 0, 1, new GUIContent("Noise Scaler"));*/
            splatMapTable = GUITableLayout.DrawTable(splatMapTable,
                                            serializedObject.FindProperty("splatHeights"));
            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewSplatHeight();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemoveSplatHeight();
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Apply SplatMaps"))
            {
                terrain.SplatMaps();
            }
        }

        showVeg = EditorGUILayout.Foldout(showVeg, "Vegetation");
        if (showVeg)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Vegetation", EditorStyles.boldLabel);
            EditorGUILayout.IntSlider(maxTrees, 0, 10000, new GUIContent("Maximum Trees"));
            EditorGUILayout.IntSlider(treeSpacing, 2, 20, new GUIContent("Trees Spacing"));
            vegMapTable = GUITableLayout.DrawTable(vegMapTable,
                                        serializedObject.FindProperty("vegetation"));
            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewVegetation();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemoveVegetation();
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Apply Vegetation"))
            {
                terrain.PlantVegetation();
            }
        }


        showSmooth = EditorGUILayout.Foldout(showSmooth, "Smooth Terrain");
        if(showSmooth)
        {
            EditorGUILayout.IntSlider(smoothAmount, 1, 10, new GUIContent("smoothAmount"));
            if (GUILayout.Button("Smooth"))
            {
                terrain.Smooth();
            }

        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        if (GUILayout.Button("Reset Terrain"))
        {
            terrain.ResetTerrain();
        }

        //Display Height Map
        showHeights = EditorGUILayout.Foldout(showHeights, "Height Map");
        if (showHeights)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            int hmtSize = (int)(EditorGUIUtility.currentViewWidth - 100);
            GUILayout.Label(hmTexture, GUILayout.Width(hmtSize), GUILayout.Height(hmtSize));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(hmtSize)))
            {
                // heightmapResolution in loc de heightmapWidth / heightmapHeight (Unity nou)
                int res = terrain.terrainData.heightmapResolution;
                float[,] heightMap = terrain.terrainData.GetHeights(0, 0, res, res);

                // textura are aceeasi marime ca heightmap-ul (daca schimb rezolutia terenului)
                if (hmTexture.width != res)
                    hmTexture = new Texture2D(res, res, TextureFormat.ARGB32, false);

                for (int y = 0; y < res; y++)
                {
                    for (int x = 0; x < res; x++)
                    {
                        hmTexture.SetPixel(x, y, new Color(heightMap[x, y], 
                                                           heightMap[x, y], 
                                                           heightMap[x, y], 1));
                    }
                }
                hmTexture.Apply();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        //Scrollbar ending code
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}