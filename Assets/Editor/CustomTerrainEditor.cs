using UnityEditor;
using EditorGUITable; // the asset guitable that i have in the project
using UnityEngine;
[CustomEditor(typeof(CustomTerrain))] // CustomTerrain and CustomTerrainEditor become linked together so we can call info between them
[CanEditMultipleObjects]
public class CustomTerrainEditor : Editor
{
    //properties-------------------
    private SerializedProperty randomHeightRange; // float in this case -> linking this with the other one in the other script
    private SerializedProperty heightMapScale;
    private SerializedProperty heightMapImage;
    private SerializedProperty perlinXScale;
    private SerializedProperty perlinYScale;
    private SerializedProperty perlinOffsetX;
    private SerializedProperty perlinOffsetY;
    private SerializedProperty perlinOctaves;
    private SerializedProperty perlinPersistance;
    private SerializedProperty perlinHeightScale;
    private SerializedProperty resetTerrain;

    private GUITableState perlinParametersTable;
    private SerializedProperty perlinParameters;
    
    //fold outs--------------------
    private bool showRandom = false; 
    private bool showLoadHeights = false;
    private bool showPerlinNoiseHeights = false;
    private bool showMultiplePerlin = false;
    
    // that s gonna run every time i enable the terrain
    private void OnEnable()
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
        
        perlinParametersTable = new GUITableState("perlinParametersTable");
        perlinParameters = serializedObject.FindProperty("perlinParameters");
    }

    public override void OnInspectorGUI() // this makes the this in inspector appear buttons sliders whatever
    {
        // always at the beginning
        serializedObject.Update(); // it updates all the serialized values between this script and the ser from customTerrain script
        
        //changing the values
        // we can access the randomHights field from customTerrain
        CustomTerrain terrain = (CustomTerrain)target; 
        // the thing is called the target is the script/class that is liked to 
        // terrain is a link to the script not to the actual terrain itself
        // I want to use serialized values so i csn change things from inspector 
        // so I'm not gonna set the terrain.random.... directly
        
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
            
            showLoadHeights = EditorGUILayout.Foldout(showLoadHeights, "Load Heights");
            if (showLoadHeights)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                GUILayout.Label("Load Heights From Texture", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(heightMapScale);
                EditorGUILayout.PropertyField(heightMapImage);
                if (GUILayout.Button("Load Texture"))
                {
                    terrain.LoadTexture();
                }
            }
            
            showPerlinNoiseHeights = EditorGUILayout.Foldout(showPerlinNoiseHeights, "Single Perlin Noise");
            if (showPerlinNoiseHeights)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                GUILayout.Label("Perlin Noise", EditorStyles.boldLabel);
                EditorGUILayout.Slider(perlinXScale, 0, 1, new GUIContent("X Scale"));
                EditorGUILayout.Slider(perlinYScale, 0, 1, new GUIContent("Y Scale"));
                EditorGUILayout.IntSlider(perlinOffsetX, 0, 10000, new GUIContent("X Offset"));
                EditorGUILayout.IntSlider(perlinOffsetY, 0, 10000, new GUIContent("Y Offset"));
                EditorGUILayout.IntSlider(perlinOctaves, 0, 10, new GUIContent("Octaves"));
                EditorGUILayout.Slider(perlinPersistance, 0.1f, 10, new GUIContent("Persistance"));
                EditorGUILayout.Slider(perlinHeightScale, 0, 1, new GUIContent("Height Scale"));
                
                if (GUILayout.Button("Perlin Noise Heights"))
                {
                    terrain.Perlin();
                }
            }

            showMultiplePerlin = EditorGUILayout.Foldout(showMultiplePerlin, "Multiple Perlin Noise");
            if (showMultiplePerlin)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                GUILayout.Label("Multiple Perlin Noise", EditorStyles.boldLabel);
                perlinParametersTable = GUITableLayout.DrawTable(perlinParametersTable,
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

                if (GUILayout.Button("Add Multiple Perlin"))
                {
                    terrain.MultiplePerlinTerrain();
                }
                
                GUILayout.Space(20);
                if (GUILayout.Button("Apply Ridge Noise"))
                {
                    terrain.RidgeNoise();
                }
            }
            
            GUILayout.Label("", EditorStyles.boldLabel);
            GUILayout.Label("Reset Heights", EditorStyles.boldLabel);
            if (GUILayout.Button("Reset Heights"))
            {
                terrain.ResetTerrain();
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
