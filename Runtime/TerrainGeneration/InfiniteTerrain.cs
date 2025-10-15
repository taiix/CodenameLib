using System.Collections.Generic;
using UnityEngine;

namespace CodenameLib.ProceduralTerrain
{
    public static class InfiniteTerrain
    {
        public static int maxViewDistance = 300;

        public static Vector2 viewerPosition;
        public static int chunksVisibleInViewDistance;

        private static readonly Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new();
        private static bool _initialized;

        // Settings and runtime
        private static TerrainSettings _baseSettings;
        private static float _chunkWorldSize;     // world-space size of a chunk (settings.size)
        private static int _indexStep;            // step in index space for noise offset (mapChunkSize - 1)
        private static UnityEngine.Transform _viewer;
        private static UnityEngine.Transform _parent;

        public static void Initialize(TerrainSettings settings, UnityEngine.Transform parent = null)
        {
            _baseSettings = settings; // struct copy
            _chunkWorldSize = Mathf.Max(1f, settings.size);
            _indexStep = Mathf.Max(1, settings.mapChunkSize - 1);
            _parent = parent;

            chunksVisibleInViewDistance = Mathf.Max(1, Mathf.RoundToInt(maxViewDistance / _chunkWorldSize));
            _initialized = true;

            Debug.Log($"[InfiniteTerrain] Initialized worldSize={_chunkWorldSize}, indexStep={_indexStep}, visibleChunks={chunksVisibleInViewDistance}");
        }

        public static void SetViewer(UnityEngine.Transform t) => _viewer = t;

        public static void UpdateViewerPosition()
        {
            if (!_initialized)
            {
                Debug.LogWarning("[InfiniteTerrain] Update called before Initialize().");
                return;
            }
            if (_viewer == null)
            {
                Debug.LogWarning("[InfiniteTerrain] Viewer not assigned.");
                return;
            }

            viewerPosition = new Vector2(_viewer.position.x, _viewer.position.z);

            // Stable chunk selection
            int currentChunkX = Mathf.FloorToInt(viewerPosition.x / _chunkWorldSize);
            int currentChunkY = Mathf.FloorToInt(viewerPosition.y / _chunkWorldSize);

            for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
            {
                for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
                {
                    Vector2 coord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

                    if (!terrainChunkDictionary.ContainsKey(coord))
                    {
                        TerrainChunk newChunk = new TerrainChunk(coord, _chunkWorldSize, _indexStep, _baseSettings, _parent);
                        terrainChunkDictionary.Add(coord, newChunk);
                    }
                }
            }

            foreach (var chunk in terrainChunkDictionary.Values)
                chunk.UpdateTerrainChunk();
        }

        public static void Clear()
        {
            foreach (var kv in terrainChunkDictionary)
            {
                if (kv.Value != null && kv.Value.chunkObject != null)
                {
                    if (Application.isPlaying) Object.Destroy(kv.Value.chunkObject);
                    else Object.DestroyImmediate(kv.Value.chunkObject);
                }
            }
            terrainChunkDictionary.Clear();
            _initialized = false;
        }

        public class TerrainChunk
        {
            public readonly Vector2 coord;
            public readonly GameObject chunkObject;

            private readonly float _worldSize;
            private readonly int _indexStep;
            private readonly TerrainSettings _settingsTemplate;
            private Bounds _bounds;
            private bool _hasMesh;

            private readonly MeshFilter _filter;
            private readonly MeshRenderer _renderer;
            private readonly MeshCollider _collider;

            public TerrainChunk(Vector2 coord, float worldSize, int indexStep, TerrainSettings baseSettings, UnityEngine.Transform parent)
            {
                this.coord = coord;
                _worldSize = worldSize;
                _indexStep = indexStep;
                _settingsTemplate = baseSettings;

                Vector3 worldPosition = new Vector3(coord.x * worldSize, 0f, coord.y * worldSize);

                chunkObject = new GameObject($"Chunk {coord.x},{coord.y}");
                if (parent != null) chunkObject.transform.SetParent(parent);
                chunkObject.transform.position = worldPosition;

                _filter = chunkObject.AddComponent<MeshFilter>();
                _renderer = chunkObject.AddComponent<MeshRenderer>();
                _collider = chunkObject.AddComponent<MeshCollider>();

                // Bounds centered on chunk with large Y for simple visibility tests
                _bounds = new Bounds(worldPosition + new Vector3(worldSize * 0.5f, 0f, worldSize * 0.5f), new Vector3(worldSize, 1000f, worldSize));

                chunkObject.SetActive(false); // will toggle visible after mesh + distance check
                GenerateMesh();
            }

            private void GenerateMesh()
            {
                // Copy settings and offset noise in index space for seamless edges
                TerrainSettings s = _settingsTemplate;
                Vector2 offsetStep = new Vector2(coord.x * _indexStep, coord.y * _indexStep);
                s.offset = _settingsTemplate.offset + offsetStep;

                MeshTerrainResult r = MeshTerrainGenerator.GenerateMeshTerrain(s);
                if (!r.success || r.mesh == null)
                {
                    Debug.LogError($"[InfiniteTerrain] Failed to generate mesh for chunk {coord}: {r.errorMessage}");
                    return;
                }

                _filter.sharedMesh = r.mesh;
                _collider.sharedMesh = r.mesh;

                if (s.drawType == TerrainDrawType.DrawPerlinNoise && r.perlinNoiseMaterial != null)
                {
                    _renderer.sharedMaterial = r.perlinNoiseMaterial;
                }
                else if (s.drawType == TerrainDrawType.DrawColorMap && r.colorMap != null)
                {
                    if (_renderer.sharedMaterial == null)
                        _renderer.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
                    _renderer.sharedMaterial.mainTexture = r.colorMap;
                }

                _hasMesh = true;
            }

            public void UpdateTerrainChunk()
            {
                if (!_hasMesh) return;

                float dist = Mathf.Sqrt(_bounds.SqrDistance(new Vector3(viewerPosition.x, 0f, viewerPosition.y)));
                bool visible = dist <= maxViewDistance;
                if (chunkObject.activeSelf != visible)
                    chunkObject.SetActive(visible);
            }
        }
    }
}
