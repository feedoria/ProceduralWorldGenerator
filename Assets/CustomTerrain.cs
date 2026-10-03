using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]

public class CustomTerrain : MonoBehaviour {
    
    public Vector2 randomHeightRange = new Vector2(0,0.1f); // the maximum and minimum hight
    // THE heightMapImage IS GONNA HOLD MY IMG
    public Texture2D heightMapImage;
    public Vector3 heightMapScale = new Vector3(1, 1, 1);

    public bool resetTerrain = true;

    // PERLIN NOISE ----------------------------------------------
    public float perlinXScale = 0.01f;
    public float perlinYScale = 0.01f;
    public int perlinOffsetX = 0;
    public int perlinOffsetY = 0;
    public int perlinOctaves = 3;
    public float perlinPersistance = 8;
    public float perlinHeightScale = 0.09f;

    // MULTIPLE PERLIN --------------------
    [System.Serializable]
    public class PerlinParameters
    {
        public float mPerlinXScale = 0.01f;
        public float mPerlinYScale = 0.01f;
        public int mPerlinOctaves = 3;
        public float mPerlinPersistance = 8;
        public float mPerlinHeightScale = 0.09f;
        public int mPerlinOffsetX = 0;
        public int mPerlinOffsetY = 0;
        public bool remove = false; 
    }

    public List<PerlinParameters> perlinParameters = new List<PerlinParameters>()
    {
        new PerlinParameters() // if there's not at least 1 line then the gui table's gonna warn me 
    };

    // Splatmaps ---------------------------------------------
    [System.Serializable]
    public class SplatHeights
    {
        public Texture2D texture = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 1.5f;
        public Vector2 tileOffset = new Vector2(0, 0);
        public Vector2 tileSize = new Vector2(50, 50);
        public float splatOffset = 0.1f;
        public float splatNoiseXScale = 0.01f;
        public float splatNoiseYScale = 0.01f;
        public float splatNoiseScaler = 0.1f;
        public bool remove = false;
    }

    public List<SplatHeights> splatHeights = new List<SplatHeights>()
    {
        new SplatHeights()
    };

    // VEGETATION -------------------------
    [System.Serializable]
    public class Vegetation
    {
        public GameObject mesh;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 90;
        public float minScale = 0.5f;
        public float maxScale = 1.0f;
        public Color colour1 = Color.white;
        public Color colour2 = Color.white;
        public Color lightColour = Color.white;
        public float minRotation = 0;
        public float maxRotation = 360;
        public float density = 0.5f;
        public bool remove = false;
    }

    public List<Vegetation> vegetation = new List<Vegetation>()
    {
        new Vegetation()
    };

    public int maxTrees = 5000;
    public int treeSpacing = 5;

    // VORONOI TESELATION -------------------
    public float voronoiFallOff = 0.2f;
    public float voronoiDropOff = 0.6f;
    public float voronoiMinHeight = 0.1f;
    public float voronoiMaxHeight = 0.5f;
    public int voronoiPeaks = 5;
    // primele 4 = ale profei (aceleasi valori), Sin si Perlin = ale mele, adaugate la final
    public enum VoronoiType { Linear = 0, Power = 1, SinPow = 2, Combined = 3, Sin = 4, Perlin = 5 }
    public VoronoiType voronoiType = VoronoiType.Linear;

    // Midpoint Displacement -> The Diamond Step
    public float MPDheightMin = -2f;
    public float MPDheightMax = 2f;
    public float MPDheightDampenerPower = 2.0f;
    public float MPDroughness = 2.0f;

    // Smooth Algorithm
    public int smoothAmount = 2;


    public Terrain terrain;
    public TerrainData terrainData;

    // heightmapResolution inlocuieste heightmapWidth / heightmapHeight (scoase din Unity-ul nou)
    int hmr { get { return terrainData.heightmapResolution; } }

    public void PlantVegetation()
    {
        TreePrototype[] newTreePrototypes;
        newTreePrototypes = new TreePrototype[vegetation.Count];
        int tindex = 0;
        foreach (Vegetation t in vegetation)
        {
            newTreePrototypes[tindex] = new TreePrototype();
            newTreePrototypes[tindex].prefab = t.mesh;
            tindex++;
        }
        terrainData.treePrototypes = newTreePrototypes;

        // daca Awake n-a rulat inca (ex. dupa recompilare), iau layer-ul dupa nume
        if (terrainLayer == -1) terrainLayer = LayerMask.NameToLayer("Terrain");

        List<TreeInstance> allVegetation = new List<TreeInstance>();
        for (int z = 0; z < terrainData.size.z; z += treeSpacing)
        {
            for (int x = 0; x < terrainData.size.x; x += treeSpacing)
            {
                for (int tp = 0; tp < terrainData.treePrototypes.Length; tp++)
                {
                    if (UnityEngine.Random.Range(0.0f, 1.0f) > vegetation[tp].density) continue;

                    // x si z sunt in metri, nu in pixeli de heightmap -> GetInterpolatedHeight cu coordonate 0-1
                    // (GetHeight(x, z) din codul profei asteapta indici de heightmap)
                    float worldHeight = terrainData.GetInterpolatedHeight(x / (float)terrainData.size.x,
                                                                          z / (float)terrainData.size.z);
                    float thisHeight = worldHeight / terrainData.size.y;
                    float thisHeightStart = vegetation[tp].minHeight;
                    float thisHeightEnd = vegetation[tp].maxHeight;

                    float steepness = terrainData.GetSteepness(x / (float)terrainData.size.x,
                                                               z / (float)terrainData.size.z);

                    if ((thisHeight >= thisHeightStart && thisHeight <= thisHeightEnd) &&
                        (steepness >= vegetation[tp].minSlope && steepness <= vegetation[tp].maxSlope))
                    {
                        TreeInstance instance = new TreeInstance();
                        instance.position = new Vector3((x + UnityEngine.Random.Range(-5.0f, 5.0f)) / terrainData.size.x,
                                                        worldHeight / terrainData.size.y,
                                                        (z + UnityEngine.Random.Range(-5.0f, 5.0f)) / terrainData.size.z);

                        Vector3 treeWorldPos = new Vector3(instance.position.x * terrainData.size.x,
                            instance.position.y * terrainData.size.y,
                            instance.position.z * terrainData.size.z)
                                                         + this.transform.position;

                        RaycastHit hit;
                        int layerMask = 1 << terrainLayer;

                        if (Physics.Raycast(treeWorldPos + new Vector3(0, 10, 0), -Vector3.up, out hit, 100, layerMask) ||
                            Physics.Raycast(treeWorldPos - new Vector3(0, 10, 0), Vector3.up, out hit, 100, layerMask))
                        {
                            float treeHeight = (hit.point.y - this.transform.position.y) / terrainData.size.y;
                            instance.position = new Vector3(instance.position.x,
                                                             treeHeight,
                                                             instance.position.z);
                        
                            instance.rotation = UnityEngine.Random.Range(vegetation[tp].minRotation, 
                                                                         vegetation[tp].maxRotation);
                            instance.prototypeIndex = tp;
                            instance.color = Color.Lerp(vegetation[tp].colour1, 
                                                        vegetation[tp].colour2, 
                                                        UnityEngine.Random.Range(0.0f,1.0f));
                            instance.lightmapColor = vegetation[tp].lightColour;
                            float s = UnityEngine.Random.Range(vegetation[tp].minScale, vegetation[tp].maxScale);
                            instance.heightScale = s;
                            instance.widthScale = s;

                            allVegetation.Add(instance);
                            if (allVegetation.Count >= maxTrees) goto TREESDONE;
                        }


                    }
                }
            }
        }
    TREESDONE:
        terrainData.treeInstances = allVegetation.ToArray();

    }

    public void AddNewVegetation()
    {
        vegetation.Add(new Vegetation()); // new row
    }

    public void RemoveVegetation()
    {
        List<Vegetation> keptVegetation = new List<Vegetation>();
        for (int i = 0; i < vegetation.Count; i++)
        {
            if (!vegetation[i].remove)
            {
                keptVegetation.Add(vegetation[i]);
            }
        }
        if (keptVegetation.Count == 0) //don't want to keep any
        {
            keptVegetation.Add(vegetation[0]); //add at least 1
        }
        vegetation = keptVegetation;
    }



    public void AddNewSplatHeight()
    {
        splatHeights.Add(new SplatHeights()); // new row
    }

    public void RemoveSplatHeight()
    {
        List<SplatHeights> keptSplatHeights = new List<SplatHeights>();
        for (int i = 0; i < splatHeights.Count; i++)
        {
            if (!splatHeights[i].remove)
            {
                keptSplatHeights.Add(splatHeights[i]);
            }
        }
        if (keptSplatHeights.Count == 0) //don't want to keep any
        {
            keptSplatHeights.Add(splatHeights[0]); //add at least 1
        }
        splatHeights = keptSplatHeights;
    }

    float GetSteepness(float[,] heightmap, int x, int y, int width, int height)
    {
        float h = heightmap[x, y];
        int nx = x + 1;
        int ny = y + 1;

        //if on the upper edge of the map find gradient by going backward.
        if (nx > width - 1) nx = x - 1;
        if (ny > height - 1) ny = y - 1;

        float dx = heightmap[nx, y] - h;
        float dy = heightmap[x, ny] - h;
        Vector2 gradient = new Vector2(dx, dy);

        float steep = gradient.magnitude;

        return steep;
    }

    public void SplatMaps()
    {
        // SplatPrototype nu mai exista in Unity-ul nou -> TerrainLayer (varianta mea)
        TerrainLayer[] newSplatPrototypes = new TerrainLayer[splatHeights.Count];
        int spindex = 0;
        foreach (SplatHeights sh in splatHeights)
        {
            newSplatPrototypes[spindex] = new TerrainLayer();
            newSplatPrototypes[spindex].diffuseTexture = sh.texture;
            newSplatPrototypes[spindex].tileOffset = sh.tileOffset;
            newSplatPrototypes[spindex].tileSize = sh.tileSize;
            newSplatPrototypes[spindex].diffuseTexture.Apply(true);
            string path = "Assets/New TerrainLayer " + spindex + ".terrainLayer";
            AssetDatabase.CreateAsset(newSplatPrototypes[spindex], path);
            spindex++;
            Selection.activeObject = this.gameObject;
        }
        terrainData.terrainLayers = newSplatPrototypes;

        float[,] heightMap = terrainData.GetHeights(0, 0, hmr, hmr);
        float[,,] splatmapData = new float[terrainData.alphamapWidth,
                                               terrainData.alphamapHeight,
                                               terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float[] splat = new float[terrainData.alphamapLayers];
                for (int i = 0; i < splatHeights.Count; i++)
                {
                    float noise = Mathf.PerlinNoise(x * splatHeights[i].splatNoiseXScale, 
                                                    y * splatHeights[i].splatNoiseYScale) 
                                       * splatHeights[i].splatNoiseScaler;
                    float offset = splatHeights[i].splatOffset + noise;
                    float thisHeightStart = splatHeights[i].minHeight - offset;
                    float thisHeightStop = splatHeights[i].maxHeight + offset;
                    //float steepness = GetSteepness(heightMap, x, y, hmr, hmr);

                    float steepness = terrainData.GetSteepness(y / (float)terrainData.alphamapHeight,
                                           x / (float)terrainData.alphamapWidth);

                    if ((heightMap[x, y] >= thisHeightStart && heightMap[x, y] <= thisHeightStop) &&
                        (steepness >= splatHeights[i].minSlope && steepness <= splatHeights[i].maxSlope))
                    {
                        splat[i] = 1;
                    }
                }
                NormalizeVector(splat);
                for (int j = 0; j < splatHeights.Count; j++)
                {
                    splatmapData[x, y, j] = splat[j];
                }
            }
        }
        terrainData.SetAlphamaps(0, 0, splatmapData); 
    }

    void NormalizeVector(float[] v)
    {
        float total = 0;
        for (int i = 0; i < v.Length; i++)
        {
            total += v[i];
        }

        if (total == 0) return; // niciun layer nu se potriveste -> evit impartirea la 0 (NaN)

        for (int i = 0; i < v.Length; i++)
        {
            v[i] /= total;
        }
    }

    float[,] GetHeightMap()
    {
        if (!resetTerrain)
        {
            return terrainData.GetHeights(0, 0, hmr, hmr);
        }
        else
            return new float[hmr, hmr];
            
    }

    List<Vector2> GenerateNeighbours(Vector2 pos, int width, int height)
    {
        List<Vector2> neighbours = new List<Vector2>();
        for (int y = -1; y < 2; y++)
        {
            for (int x = -1; x < 2; x++)
            {
                if (!(x == 0 && y == 0)) // check if i havent picked the current position -> it would be its own neighbour
                {
                    Vector2 nPos = new Vector2(Mathf.Clamp(pos.x + x, 0, width - 1),
                                                Mathf.Clamp(pos.y + y, 0, height - 1));
                    if (!neighbours.Contains(nPos))
                        neighbours.Add(nPos);
                }
            }
        }
        return neighbours;
    }

    public void Smooth()
    {
        // varianta mea: iau mereu terenul existent (cu GetHeightMap() + resetTerrain bifat ar netezi un teren plat)
        float[,] heightMap = terrainData.GetHeights(0, 0, hmr, hmr);
        float smoothProgress = 0;
        EditorUtility.DisplayProgressBar("Smoothing Terrain",
                                 "Progress",
                                 smoothProgress);

        for (int s = 0; s < smoothAmount; s++)
        {
            for (int y = 0; y < hmr; y++)
            {
                for (int x = 0; x < hmr; x++)
                {
                    // find every single pixel and getting the average of the neighbours including current pixel
                    float avgHeight = heightMap[x, y];
                    List<Vector2> neighbours = GenerateNeighbours(new Vector2(x, y), hmr, hmr);
                    foreach (Vector2 n in neighbours)
                    {
                        avgHeight += heightMap[(int)n.x, (int)n.y];
                    }

                    heightMap[x, y] = avgHeight / ((float)neighbours.Count + 1);
                }
            }
            smoothProgress++;
            EditorUtility.DisplayProgressBar("Smoothing Terrain",
                                             "Progress",
                                             smoothProgress/smoothAmount);

        }
        terrainData.SetHeights(0, 0, heightMap);
        EditorUtility.ClearProgressBar();
    }
    
    /*public void Smooth()
    {
        float[,] heightMap = GetHeightMap();
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                // find every single pixel and getting the average of the neighbours including current pixel
                float avgHeight = 0;

                if (y == 0 && x > 0 && x < width - 1)
                {
                    avgHeight = (heightMap[x, y] + 
                                heightMap[x + 1, y] +
                                heightMap[x, y + 1] +
                                heightMap[x + 1, y + 1] +
                                heightMap[x - 1, y + 1] +
                                heightMap[x - 1, y]) / 6.0f;;
                }
                else if (x == 0 && y > 0 && y < height - 1)
                {
                    avgHeight = (heightMap[x, y] +
                                 heightMap[x + 1, y] +
                                 heightMap[x + 1, y + 1] +
                                 heightMap[x + 1, y - 1] +
                                 heightMap[x, y + 1] +
                                 heightMap[x, y - 1]) / 6.0f;
                }
                else if (x == width - 1 && y > height - 1 && y < 0)
                {
                    avgHeight = (heightMap[x, y] +
                                 heightMap[x + 1, y] +
                                 heightMap[x - 1, y] +
                                 heightMap[x + 1, y - 1] +
                                 heightMap[x - 1, y - 1] +
                                 heightMap[x, y - 1]) / 6.0f;
                }
                else if (x == width - 1 && y > height - 1 && y < 0)
                {
                    avgHeight = (heightMap[x, y] +
                                 heightMap[x - 1, y] +
                                 heightMap[x - 1, y + 1] +
                                 heightMap[x - 1, y - 1] +
                                 heightMap[x, y - 1] +
                                 heightMap[x, y + 1]) / 6.0f;
                }
                else if (y > 0 && x > 0 && y < height - 1 && x < width - 1)
                {
                    avgHeight = (heightMap[x, y] +
                                 heightMap[x + 1, y] +
                                 heightMap[x - 1, y] +
                                 heightMap[x + 1, y + 1] +
                                 heightMap[x - 1, y - 1] +
                                 heightMap[x + 1, y - 1] +
                                 heightMap[x - 1, y + 1] +
                                 heightMap[x, y + 1] +
                                 heightMap[x, y - 1]) / 9.0f;
                }
                heightMap[x, y] = avgHeight;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }*/

    // taking squares out of the mesh and making them smaller and smaller 
    public void MidPointDisplacement()
    {
        float[,] heightMap = GetHeightMap();
        int width = hmr - 1;
        int squareSize = width;
        //adding some variety -> sort of and offset -> THIS IS BEFORE THE INSPECTOR VALUES
        //float height = (float) squareSize / 2.0f * 0.01f;
        //float roughness = 2.0f; // gives me control over how jagged or smooth the terrain is gonna be 
        //float heightDampener = (float)Mathf.Pow(2, -1 * roughness);
        float heightMin = MPDheightMin;
        float heightMax = MPDheightMax;
        float heightDampener = (float)Mathf.Pow(MPDheightDampenerPower, -1 * MPDroughness);


        int cornerX, cornerY;
        int midX, midY;
        int pmidXL, pmidXR, pmidYU, pmidYD; //these are for the second step -> the square step 

        // -2 is for it to be 512 -> resolution (fopr the corners)
       /* heightMap[0, 0] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[0, hmr - 2] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[hmr - 2, 0] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[hmr - 2, hmr - 2] = UnityEngine.Random.Range(0f, 0.2f);*/
        while (squareSize > 0)
        {
            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    heightMap[midX, midY] = (float)((heightMap[x, y] +
                                                     heightMap[cornerX, y] +
                                                     heightMap[x, cornerY] +
                                                     heightMap[cornerX, cornerY]) / 4.0f +
                                                    UnityEngine.Random.Range(heightMin, heightMax));
                }
            }

           for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {

                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    pmidXR = (int)(midX + squareSize);
                    pmidYU = (int)(midY + squareSize);
                    pmidXL = (int)(midX - squareSize);
                    pmidYD = (int)(midY - squareSize);

                    if (pmidXL <= 0 || pmidYD <= 0
                        || pmidXR >= width - 1 || pmidYU >= width - 1) continue;

                    // Calculate square value for the bottom side
                    heightMap[midX, y] = (float)((heightMap[midX, midY] +
                                                  heightMap[x, y] +
                                                  heightMap[midX, pmidYD] +
                                                  heightMap[cornerX, y]) / 4.0f +
                                                 UnityEngine.Random.Range(heightMin, heightMax));
                    // Calculate square value for the top side
                    heightMap[midX, cornerY] = (float)((heightMap[x, cornerY] +
                                                            heightMap[midX, midY] +
                                                            heightMap[cornerX, cornerY] +
                                                        heightMap[midX, pmidYU]) / 4.0f +
                                                       UnityEngine.Random.Range(heightMin, heightMax));

                    // Calculate square value for the left side
                    heightMap[x, midY] = (float)((heightMap[x, y] +
                                                            heightMap[pmidXL, midY] +
                                                            heightMap[x, cornerY] +
                                                  heightMap[midX, midY]) / 4.0f +
                                                 UnityEngine.Random.Range(heightMin, heightMax));
                    // Calculate square value for the right side
                    heightMap[cornerX, midY] = (float)((heightMap[midX, y] +
                                                            heightMap[midX, midY] +
                                                            heightMap[cornerX, cornerY] +
                                                            heightMap[pmidXR, midY]) / 4.0f +
                                                       UnityEngine.Random.Range(heightMin, heightMax));

                }
            }

            squareSize = (int)(squareSize / 2.0f); // dividing the square in little squares again and again until i cover up all the mesh
            heightMin *= heightDampener; // this is gonnna reduce the height
            heightMax *= heightDampener;
        }
           
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void Voronoi()
    {
        float[,] heightMap = GetHeightMap();

        for (int p = 0; p < voronoiPeaks; p++)
        {
            Vector3 peak = new Vector3(UnityEngine.Random.Range(0, hmr),
                                       UnityEngine.Random.Range(voronoiMinHeight, voronoiMaxHeight),
                                       UnityEngine.Random.Range(0, hmr)
                                      );

            if (heightMap[(int)peak.x, (int)peak.z] < peak.y)
                heightMap[(int)peak.x, (int)peak.z] = peak.y;
            else
                continue;

            Vector2 peakLocation = new Vector2(peak.x, peak.z);
            float maxDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(hmr, hmr));
            for (int y = 0; y < hmr; y++)
            {
                for (int x = 0; x < hmr; x++)
                {
                    if (!(x == peak.x && y == peak.z))
                    {
                        float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, y)) / maxDistance;
                        float h;

                        if (voronoiType == VoronoiType.Combined)
                        {
                            h = peak.y - distanceToPeak * voronoiFallOff -
                                Mathf.Pow(distanceToPeak, voronoiDropOff); //combined
                        }
                        else if (voronoiType == VoronoiType.Power)
                        {
                            h = peak.y - Mathf.Pow(distanceToPeak, voronoiDropOff) * voronoiFallOff; //power
                        }
                        else if (voronoiType == VoronoiType.SinPow)
                        {
                            h = peak.y - Mathf.Pow(distanceToPeak*3, voronoiFallOff) -
                                    Mathf.Sin(distanceToPeak*2*Mathf.PI)/voronoiDropOff; //sinpow
                        }
                        else if (voronoiType == VoronoiType.Sin)
                        {
                            h = peak.y - Mathf.Sin(distanceToPeak * 100.0f) * 0.1f; // sin
                        }
                        else if (voronoiType == VoronoiType.Perlin)
                        {
                            h = (peak.y - distanceToPeak * voronoiFallOff) +
                                Utils.fBM((x + perlinOffsetX) * perlinXScale,
                                    (y + perlinOffsetY) * perlinYScale,
                                    perlinOctaves,
                                    perlinPersistance) * perlinHeightScale; //Perlin
                        }
                        else
                        {
                            h = peak.y - distanceToPeak * voronoiFallOff; //linear
                        }

                        if(heightMap[x,y] < h)
                            heightMap[x, y] = h;
                    }

                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);

    }


    //interesting results
    //distanceToPeak = 0.5f + Mathf.Sin(distanceToPeak * Mathf.PI*0.1f)*10.0f;
    //float h = peak.y - Mathf.Log(distanceToPeak * 6 + 1) * fallOff;



    public void Perlin()
    {
        float[,] heightMap = GetHeightMap();
        // it's 2d here that's why I work w/ x and y
        for (int y = 0; y < hmr; y++)
        {
            for (int x = 0; x < hmr; x++)
            {
                // PerlinNoise requiers small values  -> first attempt (simple method)
                /*heightMap[y, x] = Mathf.PerlinNoise((x + perlinOffsetX) * perlinXScale,
                    (y + perlinOffsetY) * perlinYScale);*/
                
                heightMap[x, y] += Utils.fBM((x+perlinOffsetX) * perlinXScale, 
                                            (y+perlinOffsetY) * perlinYScale, 
                                            perlinOctaves, 
                                            perlinPersistance) * perlinHeightScale;
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    public void MultiplePerlinTerrain()
    {
        float[,] heightMap = GetHeightMap();
        for (int y = 0; y < hmr; y++)
        {
            for (int x = 0; x < hmr; x++)
            {
                foreach (PerlinParameters p in perlinParameters)
                {
                    heightMap[x, y] += Utils.fBM((x + p.mPerlinOffsetX) * p.mPerlinXScale,
                                                 (y + p.mPerlinOffsetY) * p.mPerlinYScale,
                                                    p.mPerlinOctaves,
                                                    p.mPerlinPersistance) * p.mPerlinHeightScale;
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void RidgeNoise()
    {
        ResetTerrain();
        MultiplePerlinTerrain();
        // citesc direct din teren: GetHeightMap() cu resetTerrain bifat ar da doar zerouri
        float[,] heightMap = terrainData.GetHeights(0, 0, hmr, hmr);

        for (int y = 0; y < hmr; y++)
        {
            for (int x = 0; x < hmr; x++)
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
        if (keptPerlinParameters.Count == 0) //don't want to keep any
        {
            keptPerlinParameters.Add(perlinParameters[0]); //add at least 1
        }
        perlinParameters = keptPerlinParameters;
    }

    public void RandomTerrain()
    {
        float[,] heightMap = GetHeightMap();
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
    // if I want to apply the exact texture image -> bifez resetTerrain (GetHeightMap da harta goala)
    public void LoadTexture()
    {
        float[,] heightMap = GetHeightMap();

        for (int x = 0; x < hmr; x++)
        {
            for (int z = 0; z < hmr; z++)
            {
                // the grayscale is returning a color value that I'm gettin at a certain pixel location
                // and I'm using that color to influence the height at that position 
                heightMap[x, z] += heightMapImage.GetPixel((int)(x * heightMapScale.x), 
                                                          (int)(z * heightMapScale.z)).grayscale 
                                                            * heightMapScale.y;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void ResetTerrain()
    {
        float[,] heightMap;
        heightMap = new float[hmr, hmr];
        for (int x = 0; x < hmr; x++)
        {
            for (int z = 0; z < hmr; z++)
            {
                heightMap[x, z] = 0;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);

    }

    void OnEnable()
    {
        Debug.Log("Initialising Terrain Data");
        terrain = this.GetComponent<Terrain>();
        terrainData = Terrain.activeTerrain.terrainData; // Terrain.activeTerrain = global static class 
        // i can also have Terrain.terrainData if i ve got more than one terrain in my scene
    }

    public enum TagType { Tag = 0, Layer = 1}
    int terrainLayer = -1;
    void Awake()
    {
        SerializedObject tagManager = new SerializedObject(
                              AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        AddTag(tagsProp, "Terrain", TagType.Tag);
        AddTag(tagsProp, "Cloud", TagType.Tag);
        AddTag(tagsProp, "Shore", TagType.Tag);        
        //apply tag changes to tag database
        tagManager.ApplyModifiedProperties(); // to let go into that list of tags

        SerializedProperty layerProp = tagManager.FindProperty("layers");
        terrainLayer = AddTag(layerProp, "Terrain", TagType.Layer);
        tagManager.ApplyModifiedProperties();

        //take this object
        this.gameObject.tag = "Terrain";
        if (terrainLayer != -1) // -1 = nu s-a gasit loc liber pentru layer -> Unity ar da eroare
            this.gameObject.layer = terrainLayer;
    }

    int AddTag(SerializedProperty tagsProp, string newTag, TagType tType)
    {
        bool found = false;
        //ensure the tag doesn't already exist
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(newTag)) { found = true; return i; }
        }
        //add your new tag
        if (!found && tType == TagType.Tag)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = newTag;
        }
        //add new layer
        else if(!found && tType == TagType.Layer)
        {
            for (int j = 8; j < tagsProp.arraySize; j++)
            {
                SerializedProperty newLayer = tagsProp.GetArrayElementAtIndex(j);
                //add layer in next empty slot
                if (newLayer.stringValue == "")
                {
                    newLayer.stringValue = newTag;
                    return j;
                }
            }
        }
        return -1;
    }

    // Use this for initialization
	void Start () {
        Debug.Log("Terrain Layer:" + terrainLayer);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}