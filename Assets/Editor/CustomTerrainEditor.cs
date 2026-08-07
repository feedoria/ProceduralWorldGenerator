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
    
    //fold outs--------------------
    private bool showRandom = false; 
    private bool showLoadHeights = false;
    
    // that s gonna run every time i enable the terrain
    private void OnEnable()
    {
        // the linking code
        randomHeightRange = serializedObject.FindProperty("randomHeightRange");  
        // so when we change in the inspector now we know that the values are gonna remain there
        
        heightMapScale = serializedObject.FindProperty("heightMapScale");
        heightMapImage = serializedObject.FindProperty("heightMapImage");
    }

    public override void OnInspectorGUI() // this makes the this in inspector appear buttons sliders whatever
    {
        // always at the beginning
        serializedObject.Update(); // it updates all the serialized values between this script and the ser from customTerrain script
        
        //changing the values
        // we can access the randomHights field from customTerrain
        CustomTerrain terrain = (CustomTerrain)target; // the thing is called the target is the script/class that is liked to 
        // terrain is a link to the script not to the actual terrain itself
        // I want to use serialized values so i csn change things from inspector 
        // so I'm not gonna set the terrain.random.... directly
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
