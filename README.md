# CodenameLib

CodenameLib is a robust C# library for Unity focused on:
- Fast 2D pathfinding on grid/tilemap worlds (A* and Theta*)
- Seamless Unity integration with Grid and Tilemap
- Ready-to-use agent components
- New: helper module for generating tilemap-based terrain

---

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
  - [Quick Start: PathfindingAgent2D](#quick-start-pathfindingagent2d)
  - [Direct API: Using A* or Theta*](#direct-api-using-a-or-theta)
  - [Quick Start: ThetaStarAgent](#quick-start-thetastaragent)
  - [Terrain Generation (New)](#terrain-generation-new)
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
- Procedural Terrain Generation (New): Runtime module and helpers to bootstrap grid/tilemap worlds and obstacles under `Runtime/TerrainGeneration`.

---

## Installation

CodenameLib is distributed via Unity Package Manager.

### 1. Add the Package to Unity

1. Open Unity and your project.
2. Go to `Window > Package Manager`.
3. Click the "+" button and select "Add package from Git URL..."
4. Enter the following URL:

   ```
   https://github.com/taiix/CodenameLib.git
   ```

5. Click "Add". The package will be installed and available in your project.

Note: You may need to enable "Show preview packages" in Package Manager settings if the package is not visible.

### 2. Requirements

- Unity 2021+ (recommended)
- Uses Unity's built-in `Grid` and `Tilemap` systems.

---

## Usage

### Quick Start: PathfindingAgent2D

Attach `PathfindingAgent2D` to your player or NPC GameObject and configure these fields in the Inspector:

- `grid`: Reference to your scene's Grid object.
- `obstacleTilemaps`: Array of Tilemaps marking obstacles.
- `movementSpeed`, `reachedNodeDistance`, `drawGizmos`: Tweak for behavior and visualization.
- `Player`: The transform to move.
- `Target`: The transform to reach.

Sample Script:

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

### Terrain Generation (New)

The new Terrain Generation module (see `Runtime/TerrainGeneration`) is designed to help you quickly bootstrap tilemap-based worlds for prototyping and testing with pathfinding.

- Generates or assists in generating walkable areas and obstacle layouts on `Tilemap`s.
- Works seamlessly with pathfinding: any tiles placed on obstacle tilemaps are respected by A* and Theta*.
- Intended for grid/tilemap projects using Unity’s `Grid` and `Tilemap`.

Basic workflow:

1. Create a `Grid` with child `Tilemap`s (e.g., `Ground` and `Obstacles`).
2. Use your generation logic and/or the helpers in `Runtime/TerrainGeneration` to fill the tilemaps.
3. Assign the `Obstacles` tilemap (and any others you use as blockers) to `obstacleTilemaps` on agents or in direct API calls.

Minimal example: generating an obstacle mask and running pathfinding

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;
using CodenameLib.Pathfinding;

public class GenerateAndPathfind : MonoBehaviour
{
    public Grid grid;
    public Tilemap groundTilemap;
    public Tilemap obstaclesTilemap;
    public TileBase groundTile;
    public TileBase obstacleTile;

    public Transform player;
    public Transform target;

    [Range(0f, 1f)]
    public float obstacleDensity = 0.2f;
    public int width = 64;
    public int height = 64;
    public int seed = 12345;

    void Start()
    {
        // 1) Simple procedural fill (example using Unity APIs)
        var prng = new System.Random(seed);
        groundTilemap.ClearAllTiles();
        obstaclesTilemap.ClearAllTiles();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                groundTilemap.SetTile(pos, groundTile);

                // Place obstacles with some noise/randomness
                bool blocked = prng.NextDouble() < obstacleDensity;
                if (blocked)
                    obstaclesTilemap.SetTile(pos, obstacleTile);
            }
        }

        // 2) Run pathfinding — obstaclesTilemap is respected
        var result = AStarPathfinder2D.FindPath(
            player.position,
            target.position,
            grid,
            new[] { obstaclesTilemap }
        );

        if (result.success)
        {
            Debug.Log($"Path length: {result.Path.Count}");
        }
        else
        {
            Debug.LogWarning(result.ErrorMessage);
        }
    }
}
```

Note: Explore `Runtime/TerrainGeneration` for utilities and patterns to structure generation in your project.

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

### TerrainGeneration (Module)

- Location: `Runtime/TerrainGeneration`
- Purpose: Helpers and patterns for generating tilemap-based terrain and obstacle masks for grid worlds.
- Integration: Output obstacle tilemaps can be passed to `obstacleTilemaps` in agents and direct pathfinding APIs.

---

## What’s New

- 2025-10-10: Terrain Generation module added (Runtime/TerrainGeneration) for quick tilemap world bootstrapping and obstacle layouts.
- Theta* support for smoother paths alongside A*.
