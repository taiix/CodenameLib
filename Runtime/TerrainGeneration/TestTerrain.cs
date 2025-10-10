using CodenameLib.ProceduralTerrain;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TestTerrain : MonoBehaviour
{
    [Header("Terrain Settings")]
    public TerrainSettings settings = TerrainSettings.Default;

    [Header("Generation Options")]
    public bool randomSeedEachRun = true;
    public bool autoRegenerate = true;

    [Header("Rendering")]
    public Material terrainMaterial;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private TerrainSettings _lastSettings;

    void Start()
    {
        InitializeComponents();
        GenerateTerrain();
    }

    void OnValidate()
    {
        // Auto-generate when settings change in Editor
        if (!Application.isPlaying && autoRegenerate && !settings.Equals(_lastSettings))
        {
            InitializeComponents();
            GenerateTerrain();
            _lastSettings = settings;
        }
    }

    void InitializeComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        if (terrainMaterial == null)
        {
            // Fallback material
            terrainMaterial = new Material(Shader.Find("Standard"))
            {
                color = Color.gray
            };
        }

        meshRenderer.sharedMaterial = terrainMaterial;
    }

    public void GenerateTerrain()
    {
        Debug.Log("Generating procedural terrain mesh...");

        // --- Step 1: Prepare settings
        var usedSettings = settings;
        if (randomSeedEachRun)
            usedSettings.seed = Random.Range(1, 10000);

        // --- Step 2: Generate mesh
        MeshTerrainResult result = MeshTerrainGenerator.GenerateMeshTerrain(usedSettings);

        if (!result.success)
        {
            Debug.LogError($"❌ Terrain generation failed: {result.errorMessage}");
            return;
        }

        // --- Step 3: Assign mesh & collider
        meshFilter.sharedMesh = result.mesh;
        meshCollider.sharedMesh = result.mesh;

        Debug.Log($"✅ Terrain generated! " +
                  $"Vertices: {result.mesh.vertexCount}, Seed: {usedSettings.seed}");

        // Uncomment if you want to visualize triangle edges
        // ShowDebugTriangles(result.mesh);
    }

#if UNITY_EDITOR
    void ShowDebugTriangles(Mesh mesh)
    {
        Debug.Log("Drawing debug wireframe...");
        EditorApplication.delayCall += () =>
        {
            var wireframe = new GameObject("Wireframe");
            var lineRenderer = wireframe.AddComponent<LineRenderer>();
            lineRenderer.widthMultiplier = 0.01f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.loop = false;

            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;
            Vector3[] linePoints = new Vector3[tris.Length];

            for (int i = 0; i < tris.Length; i++)
                linePoints[i] = verts[tris[i]];

            lineRenderer.positionCount = linePoints.Length;
            lineRenderer.SetPositions(linePoints);
        };
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(TestTerrain))]
public class TestTerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TestTerrain terrain = (TestTerrain)target;
        if (GUILayout.Button("Generate Terrain"))
        {
            terrain.GenerateTerrain();
        }
    }
}
#endif
