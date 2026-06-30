using System.Collections.Generic;
using UnityEngine;

namespace CodenameLib.ProceduralTerrain
{
    public static class InfiniteTerrain
    {
        public static int maxViewDistance = 300;

        public static Vector2 viewerPosition;
        public static int chunksVisibleInViewDistance;

        private static readonly Dictionary<Vector2Int, TerrainChunk> _chunks = new();

        // Runtime settings
        private static TerrainSettings _baseSettings;
        private static int _mapResolution;          // samples per side (TerrainSettings.mapChunkSize)
        private static float _chunkWorldSize;       // world units per chunk (settings.size)
        private static Transform _viewer;
        private static Transform _parent;

        private static bool _initialized;

        public static void Initialize(TerrainSettings settings, Transform parent = null)
        {
            _baseSettings    = settings; // struct copy
            _mapResolution   = settings.EffectiveResolution;
            _chunkWorldSize  = Mathf.Max(1f, settings.size);
            _parent          = parent;

            chunksVisibleInViewDistance = Mathf.Max(1, Mathf.RoundToInt(maxViewDistance / _chunkWorldSize));
            _initialized = true;

            Debug.Log($"[InfiniteTerrain] Initialized chunkWorldSize={_chunkWorldSize}, mapResolution={_mapResolution}, visibleRadius={chunksVisibleInViewDistance}");
        }

        public static void SetViewer(Transform t) => _viewer = t;

        public static void UpdateViewerPosition()
        {
            if (!_initialized) return;
            if (_viewer == null)
            {
                Debug.LogWarning("[InfiniteTerrain] Viewer not assigned.");
                return;
            }

            viewerPosition = new Vector2(_viewer.position.x, _viewer.position.z);

            int currentChunkX = Mathf.FloorToInt(viewerPosition.x / _chunkWorldSize);
            int currentChunkY = Mathf.FloorToInt(viewerPosition.y / _chunkWorldSize);

            for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
            {
                for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
                {
                    var coord = new Vector2Int(currentChunkX + xOffset, currentChunkY + yOffset);
                    if (_chunks.ContainsKey(coord)) continue;

                    var chunk = new TerrainChunk(coord, _baseSettings, _chunkWorldSize, _mapResolution, _parent);
                    _chunks.Add(coord, chunk);
                }
            }

            foreach (var c in _chunks.Values)
                c.UpdateTerrainChunk(viewerPosition, maxViewDistance);
        }

        public static void Clear()
        {
            foreach (var kv in _chunks)
            {
                if (kv.Value != null && kv.Value.chunkObject != null)
                {
                    if (Application.isPlaying) Object.Destroy(kv.Value.chunkObject);
                    else Object.DestroyImmediate(kv.Value.chunkObject);
                }
            }
            _chunks.Clear();
            _initialized = false;
        }

        // Nested for direct access to settings
        public class TerrainChunk
        {
            public readonly Vector2Int coord;
            public readonly GameObject chunkObject;

            private readonly TerrainSettings _settingsTemplate;
            private readonly float _worldSize;   // world units per side
            private readonly int _mapResolution; // samples per side

            private Bounds _bounds;
            private bool _hasMesh;

            private readonly MeshFilter _filter;
            private readonly MeshRenderer _renderer;
            private readonly MeshCollider _collider;

            public TerrainChunk(Vector2Int coord, TerrainSettings baseSettings,
                                float worldSize, int mapResolution, Transform parent)
            {
                this.coord          = coord;
                _settingsTemplate   = baseSettings;
                _worldSize          = worldSize;
                _mapResolution      = mapResolution;

                Vector3 worldOrigin = new Vector3(coord.x * _worldSize, 0f, coord.y * _worldSize);

                chunkObject = new GameObject($"Chunk {coord.x},{coord.y}");
                if (parent != null) chunkObject.transform.SetParent(parent);
                chunkObject.transform.position = worldOrigin;

                _filter   = chunkObject.AddComponent<MeshFilter>();
                _renderer = chunkObject.AddComponent<MeshRenderer>();
                _collider = chunkObject.AddComponent<MeshCollider>();

                _bounds = new Bounds(worldOrigin + new Vector3(_worldSize * 0.5f, 0f, _worldSize * 0.5f),
                                     new Vector3(_worldSize, 1000f, _worldSize));

                chunkObject.SetActive(false);
                GenerateMesh(worldOrigin);
            }

            private void GenerateMesh(Vector3 worldOrigin)
            {
                TerrainSettings s = _settingsTemplate;

                // Seamless offset: advance in world units (consistent with sampling)
                // Optional: if your noise expects sample units, convert:
                // float sampleSpacing = _worldSize / (_mapResolution - 1);
                // Vector2 noiseOffset = new Vector2(coord.x * (_mapResolution - 1) * sampleSpacing,
                //                                   coord.y * (_mapResolution - 1) * sampleSpacing);
                // s.offset = _settingsTemplate.offset + noiseOffset;

                s.offset = _settingsTemplate.offset + new Vector2(worldOrigin.x, worldOrigin.z);

                MeshTerrainResult r = MeshTerrainGenerator.GenerateMeshTerrain(s);
                if (!r.success || r.mesh == null)
                {
                    Debug.LogError($"[InfiniteTerrain] Mesh generation failed at {coord}: {r.errorMessage}");
                    return;
                }

                _filter.sharedMesh   = r.mesh;
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

            public void UpdateTerrainChunk(Vector2 viewerPos, float maxDist)
            {
                if (!_hasMesh) return;
                float dist = Mathf.Sqrt(_bounds.SqrDistance(new Vector3(viewerPos.x, 0f, viewerPos.y)));
                bool visible = dist <= maxDist;
                if (chunkObject.activeSelf != visible)
                    chunkObject.SetActive(visible);
            }
        }
    }
}
