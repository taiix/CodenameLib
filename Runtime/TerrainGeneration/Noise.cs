using UnityEngine;

namespace CodenameLib.ProceduralTerrain
{
    public static class Noise
    {
        private static ComputeShader _computeShader;

        //This is called once when the class is first accessed
        static Noise()
        {
            _computeShader = Resources.Load<ComputeShader>("ComputeShaders/TerrainCompute");
            if (_computeShader == null)
            {
                Debug.LogError("Compute Shader not found!");
            }
            Debug.Log("Compute Shader loaded successfully.");
        }

        
        public static float[,] GenerateNoiseMap(TerrainSettings settings)
        {
            int size = TerrainSettings.mapChunkSize;
            float[,] noiseMap = new float[size, size];

            if (settings.scale <= 0f)
                settings.scale = 0.0001f;

            ComputeShader computeShader = Object.Instantiate(_computeShader);

            int kernelHandle = computeShader.FindKernel("CSMain");
            ComputeBuffer noiseBuffer = new ComputeBuffer(size * size, sizeof(float));

            computeShader.SetInt("width", size);
            computeShader.SetInt("height", size);
            computeShader.SetFloat("scale", settings.scale);
            computeShader.SetInt("octaves", settings.octaves);
            computeShader.SetFloat("persistence", settings.persistence);
            computeShader.SetFloat("lacunarity", settings.lacunarity);
            computeShader.SetVector("offset", settings.offset);
            computeShader.SetInt("seed", settings.seed);

            computeShader.SetBuffer(kernelHandle, "perlinData", noiseBuffer);
            computeShader.Dispatch(kernelHandle, size / 8, size / 8, 1);

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
                        noiseMap[x, y] = 0f; // or 0.5f if you prefer midpoint
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

            return noiseMap;
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
            Material material = new Material(Shader.Find("Unlit/Texture"));
            material.mainTexture = texture;
            return material;
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
