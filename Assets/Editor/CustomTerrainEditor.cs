using UnityEditor;
using EditorGUITable; // the asset guitable that i have in the project
using UnityEngine;
[CustomEditor(typeof(CustomTerrain))] // CustomTerrain and CustomTerrainEditor become linked together so we can call info between them
[CanEditMultipleObjects]
public class CustomTerrainEditor : Editor
{
    private void OnEnable() // that s gonna run every time i enable the terrain
    {
        
    }

    public override void OnInspectorGUI() // this makes the this in inspector appear buttons sliders whatever
    {
        
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
