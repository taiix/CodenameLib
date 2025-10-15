using UnityEngine;

namespace CodenameLib.ProceduralTerrain
{

    [System.Serializable]
    public struct TerrainSettings
    {
        [Header("Starting settings")]
        public TerrainDrawType drawType;
        public bool infiniteTerrain;

        [Space]
        [Header("Noise properties")]
        public int seed;
        public float scale;
        public int octaves;
        public float persistence;
        public float lacunarity;
        public float heightMultiplier;
        public Vector2 offset;

        public const int mapChunkSize = 240;

        public float size;


        [Space]
        [Header("Island Settings")]
        public float decreasePercentage;
        public AnimationCurve edgeCurve;

        [Header("Shape Curve")]
        public AnimationCurve terrainHeightsCurve;

        [Header("Smoothing")]
        public int smoothStrength;

        [Header("Custom Area")]
        public float innerRadius;
        public float outerRadius;
        public float targetHeight;
        public int interpolationNeighbors;

        [Space]
        [Header("Terrain Types")]
        public TerrainType[] terrainTypes;

        public static int MapChunkSize => mapChunkSize;

        public static TerrainSettings Default => new TerrainSettings
        {
            drawType = TerrainDrawType.DrawColorMap,
            
            seed = 12345,
            scale = 50f,
            octaves = 4,
            persistence = 0.5f,
            lacunarity = 2f,
            heightMultiplier = 10f,
            offset = Vector2.zero,
            size = 100f,
            decreasePercentage = 0.3f,
            edgeCurve = AnimationCurve.Linear(0, 0, 1, 1),
            terrainHeightsCurve = AnimationCurve.Linear(0, 0, 1, 1),
            smoothStrength = 2,
            innerRadius = 10f,
            outerRadius = 20f,
            targetHeight = 0.5f,
            interpolationNeighbors = 2,

            terrainTypes = new[]
            {
                new TerrainType { name = "Deep Water",    height = 0.30f, color = new Color(0.00f, 0.1165323f, 1f) },
                new TerrainType { name = "Shallow Water", height = 0.40f, color = new Color(0.00f, 0.7176819f, 1f) },
                new TerrainType { name = "Sand",          height = 0.45f, color = new Color(1f, 0.9197526f, 0f) },
                new TerrainType { name = "Grass",         height = 0.55f, color = new Color(0f, 0.9197526f, 0f) },
                new TerrainType { name = "Grass 2",       height = 0.60f, color = new Color(0.08664116f, 0.5566038f, 0.08664116f) },
                new TerrainType { name = "Rock",          height = 0.70f, color = new Color(0.3962264f, 0.2201258f, 0f) },
                new TerrainType { name = "Rock 2",        height = 0.90f, color = new Color(0.2641509f, 0.1573068f, 0.08098967f) },
                new TerrainType { name = "Snow",          height = 1.00f, color = new Color(1f, 1f, 1f) }
            }
        };
    }

    public struct MeshTerrainResult
    {
        public bool success;

        public float[,] heightmap;
        public Mesh mesh;

        public Material perlinNoiseMaterial;
        public Texture2D colorMap;

        public string errorMessage;

        // Unified success factory (either material or colorMap may be null depending on drawType)
        public static MeshTerrainResult SuccessResult(
            float[,] heightmap,
            Mesh mesh,
            Material perlinNoiseMat = null,
            Texture2D colorMap = null)
        {
            return new MeshTerrainResult
            {
                success = true,
                heightmap = heightmap,
                mesh = mesh,
                perlinNoiseMaterial = perlinNoiseMat,
                colorMap = colorMap
            };
        }

        public static MeshTerrainResult Failure(string error)
        {
            return new MeshTerrainResult
            {
                success = false,
                errorMessage = error
            };
        }
    }

    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color color;
    }

    [System.Serializable]
    public enum TerrainDrawType
    {
        DrawPerlinNoise,
        DrawColorMap
    }
}
