using CodenameLib.ProceduralTerrain;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TestTerrain : MonoBehaviour
{
    [Header("Terrain Settings")]
    public TerrainSettings settings = TerrainSettings.Default;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    public Transform viewer;

    public void Generate()
    {
        MeshTerrainResult r = MeshTerrainGenerator.GenerateMeshTerrain(settings);

        if (!r.success)
        {
            Debug.LogError($"Initial terrain generation failed: {r.errorMessage}");
            return;
        }

        Debug.Log("Initial terrain generation successful.");

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        meshFilter.sharedMesh = r.mesh;
        meshCollider.sharedMesh = r.mesh;

        TerrainDrawType drawType = settings.drawType;
        if (drawType == TerrainDrawType.DrawColorMap && r.colorMap != null)
        {
            if (meshRenderer.sharedMaterial == null)
                meshRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
            meshRenderer.sharedMaterial.mainTexture = r.colorMap;
        }
        else if (drawType == TerrainDrawType.DrawPerlinNoise && r.perlinNoiseMaterial != null)
        {
            meshRenderer.sharedMaterial = r.perlinNoiseMaterial;
        }

        Debug.Log($"[TerrainGen] Mesh assigned. VertCount={r.mesh.vertexCount}, Seed={settings.seed}");
    }

    void Start()
    {
        InfiniteTerrain.Initialize(settings);
        InfiniteTerrain.SetViewer(viewer);

        Generate();
    }

    private void LateUpdate()
    {
        InfiniteTerrain.UpdateViewerPosition();
    }
    #region Comments
    //void InitializeComponents()
    //{
    //    meshFilter = GetComponent<MeshFilter>();
    //    meshRenderer = GetComponent<MeshRenderer>();
    //    meshCollider = GetComponent<MeshCollider>();

    //    if (terrainMaterial == null)
    //    {
    //        // Fallback material
    //        terrainMaterial = new Material(Shader.Find("Standard"))
    //        {
    //            color = Color.gray
    //        };
    //    }

    //    meshRenderer.sharedMaterial = terrainMaterial;
    //}

    //    public void GenerateTerrain()
    //    {
    //        Debug.Log("Generating procedural terrain mesh...");

    //        // --- Step 1: Prepare settings
    //        var usedSettings = settings;
    //        if (randomSeedEachRun)
    //            usedSettings.seed = Random.Range(1, 10000);

    //        // --- Step 2: Generate mesh
    //        MeshTerrainResult result = MeshTerrainGenerator.GenerateMeshTerrain(usedSettings);

    //        if (!result.success)
    //        {
    //            Debug.LogError($"❌ Terrain generation failed: {result.errorMessage}");
    //            return;
    //        }

    //        // --- Step 3: Assign mesh & collider
    //        meshFilter.sharedMesh = result.mesh;
    //        meshCollider.sharedMesh = result.mesh;

    //        Debug.Log($"✅ Terrain generated! " +
    //                  $"Vertices: {result.mesh.vertexCount}, Seed: {usedSettings.seed}");

    //        // Uncomment if you want to visualize triangle edges
    //        // ShowDebugTriangles(result.mesh);
    //    }

    //#if UNITY_EDITOR
    //    void ShowDebugTriangles(Mesh mesh)
    //    {
    //        Debug.Log("Drawing debug wireframe...");
    //        EditorApplication.delayCall += () =>
    //        {
    //            var wireframe = new GameObject("Wireframe");
    //            var lineRenderer = wireframe.AddComponent<LineRenderer>();
    //            lineRenderer.widthMultiplier = 0.01f;
    //            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    //            lineRenderer.loop = false;

    //            Vector3[] verts = mesh.vertices;
    //            int[] tris = mesh.triangles;
    //            Vector3[] linePoints = new Vector3[tris.Length];

    //            for (int i = 0; i < tris.Length; i++)
    //                linePoints[i] = verts[tris[i]];

    //            lineRenderer.positionCount = linePoints.Length;
    //            lineRenderer.SetPositions(linePoints);
    //        };
    //    }
    //#endif
    #endregion
}
