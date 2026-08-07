using System;
using UnityEditor;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[ExecuteInEditMode] 
public class CustomTerrain : MonoBehaviour
{
    public Vector2 randomHeightRange = new Vector2(0, 0.1f); // the maximum and minimum hight
    // 0.1f is a rlly large terrain height despite of how it appears on first look
    
    // THE heightMapImage IS GONNA HOLD MY IMG
    public Texture2D heightMapImage; 
    public Vector3 heightMapScale = new Vector3(1, 1, 1);
    
    public Terrain terrain;
    public TerrainData terrainData;
    public void RandomTerrain()
    {
        float [,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        // getting the data out of the terrain and putting it into heightMap
        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int z = 0; z < terrainData.heightmapResolution; z++)
            {
                // x = weight , z = depth 
                heightMap[x, z] += UnityEngine.Random.Range(randomHeightRange.x, randomHeightRange.y);
            }
        }
        terrainData.SetHeights(0, 0, heightMap); // setting it back into the terrain
    }
    
    // if the terrain is modified by hand or has other modifications then the texture img should be applied over 
    // the last version of the terrain
    public void LoadTextureAddHeights()
    {
        float[,] heightMap;
        heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int z = 0; z < terrainData.heightmapResolution; z++)
            {
                // the grayscale is returning a color value that I'm gettin at a certain pixel location
                // and I'm using that color to influence the height at that position 
                heightMap[x, z] += heightMapImage.GetPixel((int)(x * heightMapScale.x), 
                    (int)(z * heightMapScale.y)).grayscale * heightMapScale.y;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    
    // if I want to apply the exact texture image 
    public void LoadTexture()
    {
        float[,] heightMap;
        heightMap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution]; 

        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int z = 0; z < terrainData.heightmapResolution; z++)
            {
                // the grayscale is returning a color value that I'm gettin at a certain pixel location
                // and I'm using that color to influence the height at that position 
                heightMap[x, z] = heightMapImage.GetPixel((int)(x * heightMapScale.x), 
                    (int)(z * heightMapScale.y)).grayscale * heightMapScale.y;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void ResetTerrain()
    {
        float [,] heightMap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        terrainData.SetHeights(0, 0, heightMap);
    }

    void OnEnable()
    {
        Debug.Log("Initializing terrain data");
        terrain = this.GetComponent<Terrain>();
        terrainData = Terrain.activeTerrain.terrainData; // Terrain.activeTerrain = global static class 
        // i can also have Terrain.terrainData if i ve got more than one terrain in my scene
    }
    void Start()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        AddTag(tagsProp, "Terrain");
        AddTag(tagsProp, "Cloud");
        AddTag(tagsProp, "Shore");
        
        tagManager.ApplyModifiedProperties(); // to let go into that list of tags
        this.gameObject.tag = "Terrain";
    }

    void AddTag(SerializedProperty tagsProp, string newTag)
    {
        bool found = false;

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(newTag))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = newTag;
        }
    }
}
