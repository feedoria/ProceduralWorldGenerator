using System;
using UnityEditor;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

[ExecuteInEditMode] 
public class CustomTerrain : MonoBehaviour
{
    public Vector2 randomHeightRange = new Vector2(0, 0.1f); // the maximum and minimum hight
    // THE heightMapImage IS GONNA HOLD MY IMG
    public Texture2D heightMapImage; 
    public Vector3 heightMapScale = new Vector3(1, 1, 1);
    
    public bool resetTerrain = true;
    
    // PERLIN NOISE --------------------------
    public float perlinXScale = 0.01f;
    public float perlinYScale = 0.01f;
    public int perlinOffsetX = 0;
    public int perlinOffsetY = 0;
    public int perlinOctaves = 3;
    public float perlinPersistance = 8;
    public float perlinHeightScale = 0.09f;
    
    // MULTIPLE PERLIN -----------------------
    [System.Serializable]
    public class PerlinParameters
    {
        public float mPerlinXScale = 0.01f;
        public float mPerlinYScale = 0.01f;
        public int mPerlinOctaves = 3;
        public float mPerlinPersistance = 8;
        public float mPerlinHeightScale = 0.09f;
        public float mPerlinOffsetX = 0;
        public float mPerlinOffsetY = 0;
        public bool remove = false;
    }

    public List<PerlinParameters> perlinParameters = new List<PerlinParameters>()
    {
        new PerlinParameters() // if there's not at least 1 line then the gui table's gonna warn me 
    };
    
    // VORONOI TESELATION -------------------
    public float voronoiFallOff = 0.2f;
    public float voronoiDropOff = 0.6f;
    public float voronoiMinHeight = 0.1f;
    public float voronoiMaxHeight = 0.5f;
    public int voronoiPeaks = 5;
    public enum VoronoiType 
    { 
        Linear, 
        Power, 
        Combined, 
        Sin, 
        SinPow,
        Perlin
    }
    public VoronoiType voronoiType = VoronoiType.Linear;
    public void Voronoi()
    {
        float[,] heightMap = GetHeightMap();

        for (int p = 0; p < voronoiPeaks; p++)
        {

            Vector3 peak = new Vector3(UnityEngine.Random.Range(0, terrainData.heightmapResolution),
                                    UnityEngine.Random.Range(voronoiMinHeight, voronoiMaxHeight),
                                    UnityEngine.Random.Range(0, terrainData.heightmapResolution)
                                    );
            
            if (heightMap[(int)peak.x, (int)peak.z] < peak.y)
                heightMap[(int)peak.x, (int)peak.z] = peak.y;
            else
            {
                continue;
            }

            Vector2 peakLocation = new Vector2(peak.x, peak.z);
            float maxDistance = Vector2.Distance(new Vector2(0, 0),
                new Vector2(terrainData.heightmapResolution, terrainData.heightmapResolution));

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    if (!(x == peak.x && y == peak.z))
                    {
                        float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, y)) / maxDistance;
                        float h;
                        if (voronoiType == VoronoiType.Combined)
                            h = peak.y - distanceToPeak * voronoiFallOff - Mathf.Pow(distanceToPeak, voronoiDropOff);
                        else if (voronoiType == VoronoiType.Sin) 
                            h = peak.y - Mathf.Sin(distanceToPeak * 100.0f) * 0.1f; // sin
                        else if (voronoiType == VoronoiType.Power)
                            h = peak.y - Mathf.Pow(distanceToPeak, voronoiDropOff) * voronoiFallOff; //power
                        else if (voronoiType == VoronoiType.Linear)
                            h = peak.y - distanceToPeak * voronoiFallOff; //linear
                        else if (VoronoiType.Perlin == voronoiType)
                        {
                            h = (peak.y - distanceToPeak * voronoiFallOff) +
                                Utils.fBM((x + perlinOffsetX) * perlinXScale,
                                    (y + perlinOffsetY) * perlinYScale,
                                    perlinOctaves,
                                    perlinPersistance) * perlinHeightScale; //Perlin
                        }
                        else 
                            h = peak.y - Mathf.Pow(distanceToPeak * 3, voronoiFallOff) -
                                Mathf.Sin(distanceToPeak * 2 * Mathf.PI) / voronoiDropOff; //sinpow
                        
                        if (heightMap[x, y] < h)                       
                            heightMap[x, y] = h;
                    }
                }
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }
    
    public Terrain terrain;
    public TerrainData terrainData;
    
    int hmr { get { return terrainData.heightmapResolution; } }

    float[,] GetHeightMap()
    {
        if (!resetTerrain)
        {
            return terrainData.GetHeights(0, 0,
                terrainData.heightmapResolution,
                terrainData.heightmapResolution);
        }
        return new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
    }

    public void Perlin()
    {
        float[,] heightMap = GetHeightMap();
        // it's 2d here that's why I work w/ x and y
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                // PerlinNoise requiers small values  -> first attempt (simple method)
                /*heightMap[y, x] = Mathf.PerlinNoise((x + perlinOffsetX) * perlinXScale,
                    (y + perlinOffsetY) * perlinYScale);*/
                
                heightMap[x, y] = Utils.fBM((x + perlinOffsetX) * perlinXScale,
                                            (y + perlinOffsetY) * perlinYScale,
                                            perlinOctaves,
                                            perlinPersistance) * perlinHeightScale;
            }
        }
        
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void MultiplePerlinTerrain()
    {
        float[,] heightMap = GetHeightMap();

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                foreach (PerlinParameters perlinParameter in perlinParameters)
                {
                    heightMap[x, y] += Utils.fBM((x + perlinParameter.mPerlinOffsetX) * perlinParameter.mPerlinXScale,
                                                (y + perlinParameter.mPerlinOffsetY) * perlinParameter.mPerlinYScale,
                                                perlinParameter.mPerlinOctaves,
                                                perlinParameter.mPerlinPersistance) *  perlinParameter.mPerlinHeightScale;
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    
    public void RidgeNoise()
    {
        ResetTerrain();
        MultiplePerlinTerrain();
        float[,] heightMap = GetHeightMap();

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {

                heightMap[x, y] = 1 - Mathf.Abs(heightMap[x, y] - 0.5f);
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void AddNewPerlin()
    {
        perlinParameters.Add(new PerlinParameters());
    }

    public void RemovePerlin()
    {
        List<PerlinParameters> keptPerlinParameters = new List<PerlinParameters>();

        for (int i = 0; i < perlinParameters.Count; i++)
        {
            if (!perlinParameters[i].remove)
            {
                keptPerlinParameters.Add(perlinParameters[i]);
            }
        }

        if (keptPerlinParameters.Count == 0)
        {
            keptPerlinParameters.Add(perlinParameters[0]);
        }
        
        perlinParameters = keptPerlinParameters;
    }

    float[,] GetHeights()
    {
        return terrainData.GetHeights(0, 0, hmr, hmr);
    }
    public void RandomTerrain()
    {
        float [,] heightMap = GetHeightMap();
        // getting the data out of the terrain and putting it into heightMap
        for (int x = 0; x < hmr; x++)
        {
            for (int z = 0; z < hmr; z++)
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
        heightMap = GetHeightMap();

        for (int x = 0; x < hmr; x++)
        {
            for (int z = 0; z < hmr; z++)
            {
                // the grayscale is returning a color value that I'm gettin at a certain pixel location
                // and I'm using that color to influence the height at that position 
                heightMap[x, z] += heightMapImage.GetPixel((int)(x * heightMapScale.x), 
                    (int)(z * heightMapScale.y)).grayscale * heightMapScale.y;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    
    // if I want to apply the exact texture image -> = not +=
    public void LoadTexture()
    {
        float[,] heightMap;
        heightMap = GetHeightMap();

        for (int x = 0; x < hmr; x++)
        {
            for (int z = 0; z < hmr; z++)
            {
                // the grayscale is returning a color value that I'm gettin at a certain pixel location
                // and I'm using that color to influence the height at that position 
                heightMap[x, z] += heightMapImage.GetPixel((int)(x * heightMapScale.x), 
                    (int)(z * heightMapScale.y)).grayscale * heightMapScale.y;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void ResetTerrain()
    {
        float [,] heightMap = new float[hmr, hmr];
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
