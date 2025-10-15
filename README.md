# CodenameLib

CodenameLib is a robust C# library for Unity focused on:
- Fast 2D pathfinding on grid/tilemap worlds (A* and Theta*)
- Seamless Unity integration with Grid and Tilemap
- Ready-to-use agent components for quick movement and event hooks
- Procedural mesh terrain generation (GPU-accelerated when available)

---

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
  - [Quick Start: PathfindingAgent2D](#quick-start-pathfindingagent2d)
  - [Direct API: Using A* or Theta*](#direct-api-using-a-or-theta)
  - [Quick Start: ThetaStarAgent](#quick-start-thetastaragent)
  - [Terrain Generation API](#terrain-generation-api)
- [API Reference](#api-reference)
- [What’s New](#whats-new)

---

## Features

- A* and Theta* Algorithms: Fast, optimal pathfinding for 2D tilemaps.
- Unity Integration: Works directly with `Grid` and `Tilemap` components.
- Ready-to-Use Agents: MonoBehaviour scripts for moving objects along paths.
- Customizable Movement: Control speed, waypoints, and agent behavior.
- Path Visualization: Gizmo-based rendering of paths and waypoints in the Editor.
- Event Hooks: Subscribe to agent events for animation or logic triggers.
- Procedural Terrain Generation: Mesh-based heightmap terrain with GPU compute support, height shaping curves, smoothing, and custom radial features.

---

## Installation

CodenameLib is distributed via Unity Package Manager.

### 1) Add the Package to Unity

1. Open Unity and your project.
2. Go to `Window > Package Manager`.
3. Click the “+” button and select “Add package from Git URL…”
4. Enter the following URL:

   ```
   https://github.com/taiix/CodenameLib.git
   ```

5. Click “Add”. The package will be installed and available in your project.

Note: You may need to enable “Show preview packages” in Package Manager settings if the package is not visible.

### 2) Requirements

- Unity 2021+ (recommended)
- Pathfinding uses Unity’s built-in `Grid` and `Tilemap` systems.
- Terrain generation can optionally use a compute shader at `Resources/ComputeShaders/TerrainCompute`. If not present, a CPU fallback is used automatically.

---

## Usage

### Quick Start: PathfindingAgent2D

Attach `PathfindingAgent2D` to your player or NPC GameObject and configure these fields in the Inspector:

- `grid`: Reference to your scene’s Grid object.
- `obstacleTilemaps`: Array of Tilemaps marking obstacles.
- `movementSpeed`, `reachedNodeDistance`, `drawGizmos`: Tweak for behavior and visualization.
- `Player`: The transform to move.
- `Target`: The transform to reach.

```csharp
using CodenameLib.Pathfinding;
using UnityEngine;

public class MovementStarter : MonoBehaviour
{
    public PathfindingAgent2D agent;
    public Transform destination;

    void Start()
    {
        agent.MoveTo(destination.position);
    }
}
```

### Direct API: Using A* or Theta*

You can use the core pathfinding classes directly, without agents.

A* Example:

```csharp
using CodenameLib.Pathfinding;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AStarExample : MonoBehaviour
{
    public Grid grid;
    public Tilemap[] obstacleTilemaps;
    public Transform player;
    public Transform target;

    void Start()
    {
        PathfindingResult result = AStarPathfinder2D.FindPath(
            player.transform.position,
            target.transform.position,
            grid,
            obstacleTilemaps
        );

        if (result.success)
        {
            foreach (Vector3 waypoint in result.Path)
                Debug.Log(waypoint);
        }
        else
        {
            Debug.LogError(result.ErrorMessage);
        }
    }
}
```

Theta* Example:

```csharp
using CodenameLib.Pathfinding;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ThetaExample : MonoBehaviour
{
    public Grid grid;
    public Tilemap[] obstacleTilemaps;
    public Transform player;
    public Transform target;

    void Start()
    {
        PathfindingResult result = ThetaStarPathfinder.FindPath2D(
            player.transform.position,
            target.transform.position,
            grid,
            obstacleTilemaps
        );

        if (!result.success)
            Debug.LogWarning(result.ErrorMessage);
    }
}
```

### Quick Start: ThetaStarAgent

Attach `ThetaStarAgent` to a GameObject for advanced pathfinding and subscribe to events as needed:

```csharp
using CodenameLib.Pathfinding;
using UnityEngine;

public class ThetaMovement : MonoBehaviour
{
    public ThetaStarAgent agent;
    public Transform goal;

    void Start()
    {
        agent.OnPathComplete += OnPathFinished;
        agent.MoveTo(goal.position);
    }

    void OnPathFinished(PathfindingResult result)
    {
        if (result.success)
            Debug.Log("Path found!");
        else
            Debug.LogWarning(result.ErrorMessage);
    }
}
```

---

## Terrain Generation API

The terrain module builds a mesh from a noise heightmap and returns:
- Mesh for rendering/physics
- Heightmap Texture2D (grayscale, RFloat)
- Raw `float[,]` heightmap

It uses a GPU compute shader when available and falls back to a CPU path automatically.

- Source: [MeshTerrainGenerator.cs](https://github.com/taiix/CodenameLib/blob/main/Runtime/TerrainGeneration/MeshTerrainGenerator.cs), [TerrainData.cs](https://github.com/taiix/CodenameLib/blob/main/Runtime/TerrainGeneration/TerrainData.cs)
- Example component: [TestTerrain.cs](https://github.com/taiix/CodenameLib/blob/main/Runtime/TerrainGeneration/TestTerrain.cs)
- Compute shader (optional, for GPU path): place a compute shader at `Resources/ComputeShaders/TerrainCompute`

### Public API

- Namespace: `CodenameLib.ProceduralTerrain`

- Generation
  - `MeshTerrainResult MeshTerrainGenerator.GenerateMeshTerrain(TerrainSettings settings)`

- Settings (`TerrainSettings`)
  - `int seed` – deterministic seed
  - `float scale` – base noise scale (higher = smoother terrain)
  - `int octaves` – layers of fractal noise
  - `float persistence` – amplitude decay per octave (0–1)
  - `float lacunarity` – frequency growth per octave (>1)
  - `float heightMultiplier` – scales final height
  - `Vector2 offset` – XY noise offset
  - `int resolution` – grid resolution (vertices per axis)
  - `float size` – world-space size of mesh
  - Island shaping
    - `float decreasePercentage` – portion of the radius kept unaffected before starting edge falloff
    - `AnimationCurve edgeCurve` – edge falloff curve toward borders
  - Height shaping
    - `AnimationCurve terrainCurve` – remaps normalized height to sculpt terrain profile
  - Smoothing
    - `int smoothStrength` – smoothing radius/passes
  - Custom radial area (centered on the heightmap)
    - `float innerRadius` – fully clamped to `targetHeight`
    - `float outerRadius` – end of blend toward normal terrain
    - `float targetHeight` – height applied inside innerRadius (blends to outerRadius)
    - `int interpolationNeighbors` – neighborhood used during blend
  - Defaults: `TerrainSettings.Default` provides a sane starting configuration.

- Result (`MeshTerrainResult`)
  - `bool success`
  - `Mesh mesh`
  - `Texture2D heightmapTexture` – RFloat grayscale visualization
  - `float[,] heightmap`
  - `string errorMessage`

### Examples

Minimal: generate and assign to MeshFilter, MeshRenderer, MeshCollider

```csharp
using UnityEngine;
using CodenameLib.ProceduralTerrain;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainBootstrap : MonoBehaviour
{
    public Material material;

    void Start()
    {
        var settings = TerrainSettings.Default;
        settings.seed = 1337;
        settings.scale = 60f;
        settings.octaves = 5;
        settings.heightMultiplier = 12f;
        settings.resolution = 128;
        settings.size = 100f;

        // Optional shaping
        settings.decreasePercentage = 0.35f;                      // stronger island edges
        settings.edgeCurve = AnimationCurve.EaseInOut(0,0,1,1);   // smooth edge falloff
        settings.terrainCurve = AnimationCurve.EaseInOut(0,0,1,1);

        // Optional smoothing and a center plateau
        settings.smoothStrength = 2;
        settings.innerRadius = 8f;
        settings.outerRadius = 18f;
        settings.targetHeight = 0.6f;
        settings.interpolationNeighbors = 2;

        MeshTerrainResult result = MeshTerrainGenerator.GenerateMeshTerrain(settings);
        if (!result.success)
        {
            Debug.LogError(result.errorMessage);
            return;
        }

        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        var mc = GetComponent<MeshCollider>();

        mf.sharedMesh = result.mesh;
        mc.sharedMesh = result.mesh;

        if (material == null)
            material = new Material(Shader.Find("Standard"));
        mr.sharedMaterial = material;

        // Optional: visualize heightmap on the material
        if (result.heightmapTexture != null)
            mr.sharedMaterial.mainTexture = result.heightmapTexture;
    }
}
```

Editor workflow: use the included example component

- Add [TestTerrain.cs](https://github.com/taiix/CodenameLib/blob/main/Runtime/TerrainGeneration/TestTerrain.cs) to a GameObject.
- Tweak the `settings` in the Inspector. The mesh auto-regenerates on changes (in Edit Mode) if `autoRegenerate` is enabled.
- Click “Generate Terrain” in the custom inspector to rebuild on demand.

```csharp
// Inside TestTerrain.cs
public void GenerateTerrain()
{
    // (snip) build settings (optionally randomize seed)
    MeshTerrainResult result = MeshTerrainGenerator.GenerateMeshTerrain(usedSettings);
    if (!result.success) { Debug.LogError(result.errorMessage); return; }
    meshFilter.sharedMesh = result.mesh;
    meshCollider.sharedMesh = result.mesh;
}
```

Custom center feature (crater/plateau)

```csharp
settings.innerRadius = 6f;        // fully clamped region at center
settings.outerRadius = 16f;       // blend ends here
settings.targetHeight = 0.2f;     // crater (lower value) or plateau (higher)
settings.interpolationNeighbors = 2;
```

Accessing raw heightmap data

```csharp
MeshTerrainResult result = MeshTerrainGenerator.GenerateMeshTerrain(settings);
if (result.success)
{
    float[,] h = result.heightmap;
    int w = h.GetLength(0), d = h.GetLength(1);
    float min = float.MaxValue, max = float.MinValue;
    for (int z = 0; z < d; z++)
        for (int x = 0; x < w; x++)
        {
            float v = h[x, z];
            if (v < min) min = v;
            if (v > max) max = v;
        }
    Debug.Log($"Height range: {min:F3}..{max:F3}");
}
```

Notes and Tips:
- GPU acceleration: If a compute shader exists at `Resources/ComputeShaders/TerrainCompute`, the generator uses GPU kernels; otherwise it logs a warning and uses the CPU path automatically.
- Quality/performance:
  - `resolution` controls vertex density; costs grow roughly with resolution².
  - `size` is the world footprint; independent of vertex density.
  - `smoothStrength` increases smoothing cost; start small.
- Shaping:
  - Use `terrainCurve` to flatten lowlands or emphasize peaks without changing noise parameters.
  - `decreasePercentage` + `edgeCurve` create island-like borders.
- Output:
  - The mesh is centered at the origin and spans `size` across X/Z.
  - `heightmapTexture` uses TextureFormat.RFloat; some pipelines may need conversion for specific shaders.

---

## API Reference

### AStarPathfinder2D

- `PathfindingResult FindPath(Vector3 startWorldPos, Vector3 targetWorldPos, Grid grid, Tilemap[] obstacleTilemaps)`
  - Finds a path from start to target, avoiding obstacles.

### ThetaStarPathfinder

- `PathfindingResult FindPath2D(Vector3 startWorldPos, Vector3 targetWorldPos, Grid grid, Tilemap[] obstacleTilemaps)`
  - Uses Theta* for smoother, more direct paths.

### Pathfinding Agents

- PathfindingAgent2D (A*)
  - `void MoveTo(Vector3 targetPosition)` – Moves the assigned player to the given position.
- ThetaStarAgent (Theta*)
  - `void MoveTo(Vector3 targetPosition)`
  - `void CalculatePathOnly(Vector3 targetPosition)` – Calculates path, does not move.
  - `event Action<PathfindingResult> OnPathComplete` – Subscribe to get results.
  - `event Action OnMovementStart`, `event Action OnMovementComplete` – Subscribe for movement events.

### PathfindingResult

- `bool success` – True if a path was found.
- `List<Vector3> Path` – World positions for each waypoint.
- `string ErrorMessage` – Details when failing.

### Terrain Generation

- `MeshTerrainResult MeshTerrainGenerator.GenerateMeshTerrain(TerrainSettings settings)`
- `TerrainSettings` – configuration struct for noise, shaping, smoothing, and custom area
- `MeshTerrainResult` – success flag, `Mesh` output, `Texture2D` heightmapTexture, `float[,]` heightmap, and `errorMessage`

---

## What’s New

- 2025-10-10: Added Procedural Terrain module (mesh-based generation with optional GPU compute).
- 2025-10-08: Added Theta* pathfinding alongside A*.

---
