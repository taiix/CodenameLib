using UnityEngine;

namespace CodenameLib.ProceduralTerrain{

    [System.Serializable]
    public struct TerrainSettings
    {
        public int seed;
        public float scale;
        public int octaves;
        public float persistence;
        public float lacunarity;
        public float heightMultiplier;
        public Vector2 offset;

        public int resolution;
        public float size;

        [Header("Island Settings")]
        public float decreasePercentage;  // replaces islandSize
        public AnimationCurve edgeCurve;  // replaces edgeFalloff

        [Header("Shape Curve")]
        public AnimationCurve terrainCurve;

        [Header("Smoothing")]
        public int smoothStrength;

        [Header("Custom Area")]
        public float innerRadius;
        public float outerRadius;
        public float targetHeight;
        public int interpolationNeighbors;

        public static TerrainSettings Default => new TerrainSettings
        {
            seed = 12345,
            scale = 50f,
            octaves = 4,
            persistence = 0.5f,
            lacunarity = 2f,
            heightMultiplier = 10f,
            offset = Vector2.zero,
            resolution = 128,
            size = 100f,
            decreasePercentage = 0.3f,
            edgeCurve = AnimationCurve.Linear(0, 0, 1, 1),
            terrainCurve = AnimationCurve.Linear(0, 0, 1, 1),
            smoothStrength = 2,
            innerRadius = 10f,
            outerRadius = 20f,
            targetHeight = 0.5f,
            interpolationNeighbors = 2
        };
    }

    public struct MeshTerrainResult{
        public bool success;
        public Mesh mesh;
        public Texture2D heightmapTexture;
        public float[,] heightmap;
        public string errorMessage;

        public static MeshTerrainResult SuccessResult(Mesh mesh, Texture2D texture, float[,] heightmap){
                return new MeshTerrainResult{
                    success = true,
                    mesh = mesh,
                    heightmapTexture = texture,
                    heightmap = heightmap
                };
        }

        public static MeshTerrainResult Failure(string error) {
            return new MeshTerrainResult
            {
                success = false,
                errorMessage = error
            };
        }
        
    }
}
