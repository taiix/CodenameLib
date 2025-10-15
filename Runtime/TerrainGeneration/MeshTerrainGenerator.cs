using UnityEngine;

namespace CodenameLib.ProceduralTerrain
{
    public static class MeshTerrainGenerator
    {
        private static ComputeShader _computeShader;
        private static Texture2D _chunkTexture;

        static MeshTerrainGenerator()
        {
            LoadComputeShader();
        }

        private static void LoadComputeShader()
        {
            _computeShader = Resources.Load<ComputeShader>("ComputeShaders/TerrainCompute");

            if (_computeShader == null)
                Debug.LogWarning("[TerrainGen] ⚠️ Compute shader not found. Using CPU fallback.");
            else
                Debug.Log("[TerrainGen] ✅ Compute shader loaded successfully.");
        }

        public static MeshTerrainResult GenerateMeshTerrain(TerrainSettings settings)
        {
            try
            {
                Debug.Log($"[TerrainGen] ▶️ Generating terrain. Seed={settings.seed}");

                float[,] heightmap = Noise.GenerateNoiseMap(settings);

                Mesh mesh = CreateMeshFromHeightmap(heightmap, settings);
                
                TerrainDrawType drawType = settings.drawType;
                switch (drawType)
                {
                    case TerrainDrawType.DrawPerlinNoise:
                        Material perlinMat = Noise.DrawNoiseMap(heightmap);
                        return MeshTerrainResult.SuccessResult(heightmap, mesh, perlinMat);

                    case TerrainDrawType.DrawColorMap:
                        Texture2D colorMap = Noise.DrawColorMap(heightmap, settings);
                        return MeshTerrainResult.SuccessResult(heightmap, mesh, null, colorMap);
                }

                return MeshTerrainResult.Failure("Unknown draw type.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TerrainGen] ❌ Generation failed: {e}");
                return MeshTerrainResult.Failure(e.Message);
            }
        }

        private static Mesh CreateMeshFromHeightmap(float[,] heightmap, TerrainSettings settings)
        {
            int width = TerrainSettings.mapChunkSize;
            int height = TerrainSettings.mapChunkSize;
            float size = settings.size;

            Debug.Log("[TerrainGen] 🧱 Creating mesh...");
            Mesh mesh = new Mesh { name = "ProceduralTerrain" };

            if (width * height > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            Vector3[] verts = new Vector3[width * height];
            Vector2[] uvs = new Vector2[width * height];
            int[] tris = new int[(width - 1) * (height - 1) * 6];

            // Build vertices & UVs
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = x + z * width;
                    float xPos = (x / (float)(width - 1) - 0.5f) * size;
                    float zPos = (z / (float)(height - 1) - 0.5f) * size;
                    float yPos = settings.terrainHeightsCurve.Evaluate(heightmap[x, z])
                        * settings.heightMultiplier;

                    verts[i] = new Vector3(xPos, yPos, zPos);
                    uvs[i] = new Vector2(
                        x / (float)(width - 1),
                        z / (float)(height - 1));
                }
            }

            // Build triangle indices
            int t = 0;
            for (int z = 0; z < height - 1; z++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int bl = x + z * width;
                    int br = (x + 1) + z * width;
                    int tl = x + (z + 1) * width;
                    int tr = (x + 1) + (z + 1) * width;

                    tris[t] = bl; tris[t + 1] = tl; tris[t + 2] = tr;
                    tris[t + 3] = bl; tris[t + 4] = tr; tris[t + 5] = br;
                    t += 6;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Debug.Log($"[TerrainGen] ✅ Mesh created. Vertices={verts.Length}, Tris={tris.Length / 3}");
            return mesh;
        }

        #region Implementation Commented Out
        //private static float[,] GenerateHeightmapWithComputeShader(TerrainSettings settings)
        //{
        //    int resolution = settings.resolution;
        //    float[,] heightmap = new float[resolution, resolution];

        //    if (_computeShader == null)
        //    {
        //        Debug.LogWarning("[TerrainGen] ⚠️ ComputeShader missing. Using CPU fallback...");
        //        return GenerateHeightmapCPU(settings);
        //    }

        //    Debug.Log("[TerrainGen] ▶️ Starting GPU heightmap pipeline...");

        //    ComputeShader computeShader = Object.Instantiate(_computeShader);
        //    ComputeBuffer perlinData = new ComputeBuffer(resolution * resolution, sizeof(float));
        //    ComputeBuffer smoothingData = new ComputeBuffer(resolution * resolution, sizeof(float));
        //    ComputeBuffer terrainHeightData = new ComputeBuffer(resolution * resolution, sizeof(float));
        //    ComputeBuffer customAreaData = new ComputeBuffer(resolution * resolution, sizeof(float));

        //    try
        //    {
        //        // Step 1: Generate Perlin Noise
        //        GeneratePerlinNoiseGPU(computeShader, settings, perlinData);
        //        Debug.Log("[TerrainGen] 🌀 Perlin noise GPU dispatched.");

        //        float[] noiseData = new float[resolution * resolution];
        //        perlinData.GetData(noiseData);

        //        float minNoise = float.MaxValue, maxNoise = float.MinValue;
        //        for (int i = 0; i < noiseData.Length; i++)
        //        {
        //            if (noiseData[i] < minNoise) minNoise = noiseData[i];
        //            if (noiseData[i] > maxNoise) maxNoise = noiseData[i];
        //        }

        //        Debug.Log($"[TerrainGen] Noise range: {minNoise:F4} → {maxNoise:F4}");

        //        float[,] initHeight = ApplyIslandAndHeight(noiseData, settings, resolution);
        //        ValidateHeights("After island mask", initHeight);

        //        // Step 2: Smooth
        //        terrainHeightData.SetData(initHeight);
        //        SmoothTerrainGPU(computeShader, terrainHeightData, smoothingData, settings.smoothStrength, resolution);
        //        Debug.Log("[TerrainGen] 💧 Smoothing pass dispatched.");

        //        float[] smoothedData = new float[resolution * resolution];
        //        smoothingData.GetData(smoothedData);

        //        float[,] smoothedHeightmap = new float[resolution, resolution];
        //        for (int z = 0; z < resolution; z++)
        //            for (int x = 0; x < resolution; x++)
        //                smoothedHeightmap[x, z] = smoothedData[z * resolution + x];
        //        ValidateHeights("After smoothing", smoothedHeightmap);

        //        // Step 3: Apply Custom Area
        //        terrainHeightData.SetData(smoothedData);
        //        ApplyCustomAreaGPU(computeShader, terrainHeightData, customAreaData, settings, resolution);
        //        Debug.Log("[TerrainGen] 🎯 Custom area pass dispatched.");

        //        float[] finalData = new float[resolution * resolution];
        //        customAreaData.GetData(finalData);

        //        for (int z = 0; z < resolution; z++)
        //            for (int x = 0; x < resolution; x++)
        //                heightmap[x, z] = finalData[z * resolution + x];

        //        ValidateHeights("Final heightmap", heightmap);
        //    }
        //    finally
        //    {
        //        perlinData.Release();
        //        smoothingData.Release();
        //        terrainHeightData.Release();
        //        customAreaData.Release();
        //        Object.DestroyImmediate(computeShader);
        //    }

        //    return heightmap;
        //}

        //private static void GeneratePerlinNoiseGPU(
        //    ComputeShader computeShader, TerrainSettings settings,
        //    ComputeBuffer perlinData)
        //{
        //    int kernelIndex = computeShader.FindKernel("CSMain");

        //    computeShader.SetBuffer(kernelIndex, "perlinData", perlinData);
        //    computeShader.SetInt("width", settings.resolution);
        //    computeShader.SetInt("height", settings.resolution);
        //    computeShader.SetInt("seed", settings.seed);
        //    computeShader.SetFloat("scale", settings.scale);
        //    computeShader.SetInt("octaves", settings.octaves);
        //    computeShader.SetFloat("persistence", settings.persistence);
        //    computeShader.SetFloat("lacunarity", settings.lacunarity);
        //    computeShader.SetVector("offset", settings.offset);

        //    int groups = Mathf.CeilToInt(settings.resolution / 8f);
        //    computeShader.Dispatch(kernelIndex, groups, groups, 1);
        //}

        //private static void SmoothTerrainGPU(
        //    ComputeShader computeShader, ComputeBuffer inputData,
        //    ComputeBuffer outputData, int smoothStrength, int resolution)
        //{
        //    int kernelIndex = computeShader.FindKernel("SmoothingWholeTerrain");

        //    computeShader.SetBuffer(kernelIndex, "smoothingData", outputData);
        //    computeShader.SetBuffer(kernelIndex, "terrainHeightsData", inputData);
        //    computeShader.SetInt("smoothRadius", smoothStrength);

        //    int groups = Mathf.CeilToInt(resolution / 8f);
        //    computeShader.Dispatch(kernelIndex, groups, groups, 1);
        //}

        //private static void ApplyCustomAreaGPU(
        //    ComputeShader computeShader, ComputeBuffer inputData,
        //    ComputeBuffer outputData, TerrainSettings settings, int resolution)
        //{
        //    int kernelIndex = computeShader.FindKernel("SmoothingCustomArea");

        //    computeShader.SetBuffer(kernelIndex, "customAreaData", outputData);
        //    computeShader.SetBuffer(kernelIndex, "terrainHeightsData", inputData);

        //    int centerX = resolution / 2;
        //    int centerZ = resolution / 2;

        //    computeShader.SetInt("customAreaX", centerX);
        //    computeShader.SetInt("customAreaZ", centerZ);
        //    computeShader.SetInt("interpolationNeightbours", settings.interpolationNeighbors);
        //    computeShader.SetFloat("innerRadius", settings.innerRadius);
        //    computeShader.SetFloat("outerRadius", settings.outerRadius);
        //    computeShader.SetFloat("targetHeight", settings.targetHeight);

        //    int groups = Mathf.CeilToInt(resolution / 8f);
        //    computeShader.Dispatch(kernelIndex, groups, groups, 1);
        //}

        //private static float[,] ApplyIslandAndHeight(float[] noiseData, TerrainSettings settings, int resolution)
        //{
        //    float[,] heights = new float[resolution, resolution];
        //    float[,] edgeReduction = CalculateIslandBorders(resolution, settings);

        //    for (int z = 0; z < resolution; z++)
        //    {
        //        for (int x = 0; x < resolution; x++)
        //        {
        //            int index = x + z * resolution;

        //            float height = noiseData[index];
        //            height *= edgeReduction[x, z];

        //            // Apply curve shaping if available
        //            if (settings.terrainCurve != null)
        //                height = settings.terrainCurve.Evaluate(height);

        //            // Apply multiplier last
        //            heights[x, z] = height * settings.heightMultiplier;
        //        }
        //    }

        //    return heights;
        //}

        //private static float[,] CalculateIslandBorders(int resolution, TerrainSettings settings)
        //{
        //    float[,] mask = new float[resolution, resolution];

        //    int half = resolution / 2;
        //    Vector2Int center = new Vector2Int(half, half);

        //    float unaffectedBorder = settings.decreasePercentage * half;
        //    float areaToAffect = half - unaffectedBorder;

        //    for (int z = 0; z < resolution; z++)
        //    {
        //        for (int x = 0; x < resolution; x++)
        //        {
        //            float distance = Vector2.Distance(center, new Vector2(x, z)) - unaffectedBorder;

        //            if (distance < 0)
        //                mask[x, z] = 1;
        //            else if (distance > areaToAffect)
        //                mask[x, z] = 0;
        //            else
        //                mask[x, z] = settings.edgeCurve.Evaluate(1 - distance / areaToAffect);
        //        }
        //    }

        //    return mask;
        //}

        //private static float[,] GenerateHeightmapCPU(TerrainSettings settings)
        //{
        //    int res = settings.resolution;
        //    float[,] map = new float[res, res];

        //    // generate raw noise (multi-octave)
        //    float amplitude = 1f, frequency = 1f, maxAmp = 0f;
        //    for (int z = 0; z < res; z++)
        //    {
        //        for (int x = 0; x < res; x++)
        //        {
        //            float noise = 0f;
        //            amplitude = 1f; frequency = 1f; maxAmp = 0f;
        //            for (int o = 0; o < settings.octaves; o++)
        //            {
        //                float xCoord = (x / (float)res) * settings.scale * frequency + settings.offset.x + settings.seed;
        //                float zCoord = (z / (float)res) * settings.scale * frequency + settings.offset.y + settings.seed;
        //                noise += Mathf.PerlinNoise(xCoord, zCoord) * amplitude;
        //                maxAmp += amplitude;
        //                amplitude *= settings.persistence;
        //                frequency *= settings.lacunarity;
        //            }
        //            noise /= Mathf.Max(1e-6f, maxAmp);

        //            map[x, z] = noise;
        //        }
        //    }

        //    // apply edge reduction using your CalculateIslandBorders-like function
        //    float[,] edgeMask = CalculateIslandBorders(res, settings);
        //    for (int z = 0; z < res; z++)
        //        for (int x = 0; x < res; x++)
        //        {
        //            float h = map[x, z] * edgeMask[x, z];
        //            if (settings.terrainCurve != null) h = settings.terrainCurve.Evaluate(h);
        //            map[x, z] = h * settings.heightMultiplier;
        //        }

        //    ValidateHeights("CPU fallback", map);
        //    return map;
        //}

        //private static Mesh CreateMeshFromHeightmap(float[,] heightmap, TerrainSettings settings)
        //{
        //    int res = settings.resolution;
        //    float size = settings.size;

        //    Debug.Log("[TerrainGen] 🧱 Creating mesh...");
        //    Mesh mesh = new Mesh { name = "ProceduralTerrain" };

        //    Vector3[] verts = new Vector3[res * res];
        //    Vector2[] uvs = new Vector2[res * res];
        //    int[] tris = new int[(res - 1) * (res - 1) * 6];

        //    for (int z = 0; z < res; z++)
        //    {
        //        for (int x = 0; x < res; x++)
        //        {
        //            int i = x + z * res;
        //            float xPos = (x / (float)(res - 1) - 0.5f) * size;
        //            float zPos = (z / (float)(res - 1) - 0.5f) * size;
        //            float yPos = heightmap[x, z];
        //            verts[i] = new Vector3(xPos, yPos, zPos);
        //            uvs[i] = new Vector2(x / (float)res, z / (float)res);
        //        }
        //    }

        //    int t = 0;
        //    for (int z = 0; z < res - 1; z++)
        //    {
        //        for (int x = 0; x < res - 1; x++)
        //        {
        //            int bl = x + z * res;
        //            int br = (x + 1) + z * res;
        //            int tl = x + (z + 1) * res;
        //            int tr = (x + 1) + (z + 1) * res;

        //            tris[t] = bl; tris[t + 1] = tl; tris[t + 2] = tr;
        //            tris[t + 3] = bl; tris[t + 4] = tr; tris[t + 5] = br;
        //            t += 6;
        //        }
        //    }

        //    mesh.vertices = verts;
        //    mesh.uv = uvs;
        //    mesh.triangles = tris;
        //    mesh.RecalculateNormals();
        //    mesh.RecalculateBounds();

        //    Debug.Log($"[TerrainGen] ✅ Mesh created. Vertices={verts.Length}, Tris={tris.Length / 3}");
        //    return mesh;
        //}

        //private static Texture2D CreateHeightmapTexture(float[,] map)
        //{
        //    int w = map.GetLength(0);
        //    int h = map.GetLength(1);

        //    Texture2D tex = new Texture2D(w, h, TextureFormat.RFloat, false)
        //    {
        //        filterMode = FilterMode.Bilinear,
        //        wrapMode = TextureWrapMode.Clamp
        //    };

        //    for (int z = 0; z < h; z++)
        //        for (int x = 0; x < w; x++)
        //        {
        //            float v = map[x, z];
        //            tex.SetPixel(x, z, new Color(v, v, v));
        //        }

        //    tex.Apply();
        //    Debug.Log("[TerrainGen] 🖼️ Heightmap texture generated.");
        //    return tex;
        //}

        //private static void ValidateHeights(string label, float[,] map)
        //{
        //    float min = float.MaxValue, max = float.MinValue;
        //    int w = map.GetLength(0), h = map.GetLength(1);
        //    for (int z = 0; z < h; z++)
        //        for (int x = 0; x < w; x++)
        //        {
        //            float v = map[x, z];
        //            if (v < min) min = v;
        //            if (v > max) max = v;
        //        }
        //    Debug.Log($"[TerrainGen] {label}: min={min:F3}, max={max:F3}, range={(max - min):F3}");
        //}
        #endregion
    }
}
