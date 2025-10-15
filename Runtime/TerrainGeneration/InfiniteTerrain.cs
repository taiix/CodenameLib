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
        private static int _mapChunkSize;
        // Settings and runtime
        private static TerrainSettings _baseSettings;

        private static Transform _viewer;
        private static Transform _parent;

        public static void Initialize(TerrainSettings settings, Transform parent = null)
        {
            _baseSettings = settings; // struct copy
            _mapChunkSize = settings.mapChunkSize;
            _parent = parent;

            chunksVisibleInViewDistance = Mathf.Max(1, Mathf.RoundToInt(maxViewDistance / _mapChunkSize));

            Debug.Log($"[InfiniteTerrain] Initialized worldSize={_chunkWorldSize}, indexStep={_indexStep}, visibleChunks={chunksVisibleInViewDistance}");
        }

        public static void SetViewer(Transform t) => _viewer = t;

        public static void UpdateViewerPosition()
        {
            if (_viewer == null)
            {
                Debug.LogWarning("[InfiniteTerrain] Viewer not assigned.");
                return;
            }

            viewerPosition = new Vector2(_viewer.position.x, _viewer.position.z);

            // Stable chunk selection
            int currentChunkX = Mathf.FloorToInt(viewerPosition.x / _mapChunkSize);
            int currentChunkY = Mathf.FloorToInt(viewerPosition.y / _mapChunkSize);

            for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
            {
                for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
                {
                    Vector2 coord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

                    if (!terrainChunkDictionary.ContainsKey(coord))
                    {
                        TerrainChunk newChunk = new TerrainChunk(coord, _baseSettings, _parent);
                        terrainChunkDictionary.Add(coord, newChunk);
                    }
                }
            }

                foreach (var chunk in terrainChunkDictionary.Values)
                    chunk.UpdateTerrainChunk();
        }
    }

        public class TerrainChunk
        {
            public readonly Vector2 coord;
            public readonly GameObject chunkObject;

            
            private readonly TerrainSettings _settingsTemplate;
            private Bounds _bounds;
            private bool _hasMesh;

            private int _mapChunkSize;

            private readonly MeshFilter _filter;
            private readonly MeshRenderer _renderer;
            private readonly MeshCollider _collider;

            public TerrainChunk(Vector2 coord, TerrainSettings baseSettings, Transform parent)
            {
                this.coord = coord;
             
                _settingsTemplate = baseSettings;
                _mapChunkSize = _settingsTemplate.mapChunkSize;
                
                Vector3 worldPosition = new Vector3(coord.x * _mapChunkSize, 0f, coord.y * _mapChunkSize);

                chunkObject = new GameObject($"Chunk {coord.x},{coord.y}");
                if (parent != null) chunkObject.transform.SetParent(parent);
                chunkObject.transform.position = worldPosition;

                _filter = chunkObject.AddComponent<MeshFilter>();
                _renderer = chunkObject.AddComponent<MeshRenderer>();
                _collider = chunkObject.AddComponent<MeshCollider>();

                // Bounds centered on chunk with large Y for simple visibility tests
                _bounds = new Bounds(worldPosition + new Vector3(_mapChunkSize * 0.5f, 0f, _mapChunkSize * 0.5f), new Vector3(_mapChunkSize, 1000f, _mapChunkSize));

                chunkObject.SetActive(false); // will toggle visible after mesh + distance check
                GenerateMesh();
            }

            private void GenerateMesh()
            {
                // Copy settings and offset noise in index space for seamless edges
                TerrainSettings s = _settingsTemplate;
                Vector2 offsetStep = new Vector2(coord.x * _mapChunkSize, coord.y * _mapChunkSize);
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
                                float dist = Mathf.Sqrt(_bounds.SqrDistance(new Vector3(viewerPosition.x, 0f, viewerPosition.y)));
                bool visible = dist <= maxViewDistance;
                if (chunkObject.activeSelf != visible)
                    chunkObject.SetActive(visible);
            }
        }
    }
}
