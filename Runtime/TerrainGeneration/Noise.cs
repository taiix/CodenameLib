using UnityEngine;

namespace CodenameLib.ProceduralTerrain
{
    public static class Noise
    {
        private static ComputeShader _computeShader;
        private static Material _terrainMaterial;
        //This is called once when the class is first accessed
        static Noise()
        {
            _computeShader = Resources.Load<ComputeShader>("ComputeShaders/TerrainCompute");
            _terrainMaterial = Resources.Load<Material>("TerrainShaders/TerrainMaterial");
            if (_computeShader == null)
            {
                Debug.LogError("Compute Shader not found!");
            }
            if (_terrainMaterial == null)
            {
                Debug.LogError("Terrain Material not found!");
            }
            Debug.Log("Compute Shader loaded successfully.");
            Debug.Log("Terrain Shader loaded successfully.");
        }


        public static float[,] GenerateNoiseMap(TerrainSettings settings)
        {
            int size = settings.EffectiveResolution;
            float[,] noiseMap = new float[size, size];

            if (settings.scale <= 0f)
                settings.scale = 0.0001f;

            ComputeShader computeShader = Object.Instantiate(_computeShader);

            int kernelHandle = computeShader.FindKernel("CSMain");
            ComputeBuffer noiseBuffer = new ComputeBuffer(size * size, sizeof(float));
            ComputeBuffer smoothingData = new ComputeBuffer(size * size, sizeof(float));
            ComputeBuffer terrainHeightsData = new ComputeBuffer(size * size, sizeof(float));

            computeShader.SetInt("width", size);
            computeShader.SetInt("height", size);
            computeShader.SetFloat("scale", settings.scale);
            computeShader.SetInt("octaves", settings.octaves);
            computeShader.SetFloat("persistence", settings.persistence);
            computeShader.SetFloat("lacunarity", settings.lacunarity);
            computeShader.SetVector("offset", settings.offset);
            computeShader.SetInt("seed", settings.seed);

            computeShader.SetBuffer(kernelHandle, "perlinData", noiseBuffer);

            int threadGroupsX = Mathf.CeilToInt(size / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(size / 8.0f);
            computeShader.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);

            float[] noiseArray = new float[size * size];
            noiseBuffer.GetData(noiseArray);

            noiseBuffer.Release();


            float min = float.MaxValue;
            float max = float.MinValue;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float v = noiseArray[y * size + x];
                    noiseMap[x, y] = v;


                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }

            float range = max - min;
            if (range <= Mathf.Epsilon)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                        noiseMap[x, y] = 0f;
                }
                return noiseMap;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalized = (noiseMap[x, y] - min) / range;
                    noiseMap[x, y] = normalized;
                }
            }

            noiseMap = WholeTerrainSmoothing(computeShader, smoothingData, terrainHeightsData, size, size, noiseMap, settings.smoothStrength);

            smoothingData.Release();
            terrainHeightsData.Release();

            Object.DestroyImmediate(computeShader);

            return noiseMap;
        }

        private static float[,] WholeTerrainSmoothing(ComputeShader computeShader, ComputeBuffer smoothingData, ComputeBuffer terrainHeightsData, int width, int height, float[,] noiseHeights, int smoothStrenght)
        {
            int kernelIndexSmoothing = computeShader.FindKernel("SmoothingWholeTerrain");

            computeShader.SetBuffer(kernelIndexSmoothing, "smoothingData", smoothingData);

            terrainHeightsData.SetData(noiseHeights);
            computeShader.SetBuffer(kernelIndexSmoothing, "terrainHeightsData", terrainHeightsData);

            computeShader.SetInt("smoothRadius", smoothStrenght);

            computeShader.Dispatch(kernelIndexSmoothing, width / 8, height / 8, 1);

            float[] smoothedHeightsData = new float[width * height];
            smoothingData.GetData(smoothedHeightsData);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    noiseHeights[x, y] = smoothedHeightsData[index];
                }
            }

            return noiseHeights;
        }

        //Generates a noise map texture aka perlin noise material
        public static Material DrawNoiseMap(float[,] noiseMap)
        {
            int width = noiseMap.GetLength(0);
            int height = noiseMap.GetLength(1);

            Texture2D texture = new Texture2D(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = noiseMap[x, y];
                    texture.SetPixel(x, y, new Color(value, value, value));
                }
            }
            texture.Apply();
            _terrainMaterial.SetTexture("_HeightMap", texture);
            return _terrainMaterial;
        }

        //Applies colors based on the terrain heights from noisemap
        public static Texture2D DrawColorMap(float[,] noiseMap, TerrainSettings data)
        {
            int width = noiseMap.GetLength(0);
            int height = noiseMap.GetLength(1);

            Color[] colorMap = new Color[width * height];

            TerrainType[] regions = data.terrainTypes;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = noiseMap[x, y];
                    for (int i = 0; i < regions.Length; i++)
                    {
                        if (value <= regions[i].height)
                        {
                            colorMap[y * width + x] = regions[i].color;
                            break;
                        }
                    }
                }
            }

            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(colorMap);
            texture.Apply();

            return texture;
        }
    }
}
