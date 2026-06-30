using UnityEngine;

namespace CodenameLib.ProceduralTerrain
{
    public static class MeshTerrainGenerator
    {
        private static ComputeShader _computeShader;
        private static Texture2D _chunkTexture;

        //Called once the first time the class is called
        static MeshTerrainGenerator()
        {
            LoadComputeShader();
        }

        //Load the compute shader
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

                if (settings.erosion.enabled)
                {
                    Debug.Log($"[TerrainGen] 💧 Eroding. Droplets={settings.erosion.iterations}");
                    GpuErosion.Erode(heightmap, settings.erosion);
                }

                Mesh mesh = CreateMeshFromHeightmap(heightmap, settings);
                CreateHeightMap(heightmap);

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

        private static Texture2D CreateHeightMap(float[,] heights)
        {
            int height = heights.GetLength(0);
            int width = heights.GetLength(1);

            Texture2D heightmapTexture = new Texture2D(width, height, TextureFormat.RFloat, false);

            heightmapTexture.filterMode = FilterMode.Trilinear;
            heightmapTexture.wrapMode = TextureWrapMode.Repeat;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float heightValue = (heights[y, x]);
                    heightmapTexture.SetPixel(x, y, new Color(heightValue, heightValue, heightValue));
                }
            }

            heightmapTexture.Apply();
            _chunkTexture = heightmapTexture;
            return _chunkTexture;
        }

        public static Texture2D GetHeightMapTexture()
        {
            return _chunkTexture;
        }

        //Create the actual mesh based on the heightmap from the compute shader
        private static Mesh CreateMeshFromHeightmap(float[,] heightmap, TerrainSettings settings)
        {
            int width = settings.EffectiveResolution;
            int height = settings.EffectiveResolution;
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


    }
}
