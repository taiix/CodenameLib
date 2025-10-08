# CodenameLib

CodenameLib is a C# library focused on providing 2D pathfinding solutions using the A* and Theta* algorithms, designed for Unity projects that use grid-based movement and tilemaps. This guide explains the main features and usage patterns for normal users who want to integrate efficient pathfinding into their games or applications.

## Features

- **A* and Theta* Pathfinding Algorithms**: Find optimal paths on grid-based maps with support for obstacles.
- **Unity Integration**: Built to work seamlessly with Unity's `Grid` and `Tilemap` systems.
- **Pathfinding Agents**: Ready-made MonoBehaviour agents to move characters along calculated paths.
- **Customizable Movement and Visualization**: Control movement speed, path-following behavior, and path visualization with Gizmos.

## Installation

1. Clone or download the repository:  
   [taiix/CodenameLib](https://github.com/taiix/CodenameLib)
2. Copy the `Runtime/Pathfinding` folder into your Unity project's Assets directory.
3. Ensure you have references to UnityEngine, UnityEngine.Tilemaps, and UnityEngine.Grid in your project.

## Getting Started

### Example: Using the PathfindingAgent2D

Attach the `PathfindingAgent2D` script to your player or NPC GameObject. Set up references in the Unity Inspector:

```csharp
using CodenameLib.Pathfinding;

public class MyMovementController : MonoBehaviour
{
    public PathfindingAgent2D agent;
    public Transform target;

    void Start()
    {
        agent.MoveTo(target.position);
    }
}
```

The `PathfindingAgent2D` will use A* to find a path from its position to the target, avoiding obstacles set in the provided Tilemaps.

**Inspector Setup:**
- **Grid**: Reference to your scene's Grid object.
- **Obstacle Tilemaps**: Array of Tilemaps marking impassable tiles.
- **Movement Speed**: How fast the agent moves.
- **Reached Node Distance**: How close the agent must get to a waypoint before moving to the next.
- **Draw Gizmos**: Enable to visualize the calculated path in the Editor.

### Example: Direct Use of A* or Theta* Finder

Use the static methods to calculate paths without agents:

```csharp
using CodenameLib.Pathfinding;

// Using A* pathfinding
PathfindingResult result = AStarPathfinder2D.FindPath(
    startWorldPos, targetWorldPos, grid, obstacleTilemaps);

if (result.success)
{
    foreach (Vector3 waypoint in result.Path)
    {
        // Move your object or visualize the path
    }
}
else
{
    Debug.LogError("Pathfinding failed: " + result.ErrorMessage);
}

// Using Theta* pathfinding
PathfindingResult thetaResult = ThetaStarPathfinder.FindPath2D(
    startWorldPos, targetWorldPos, grid, obstacleTilemaps);
```

### Example: Using ThetaStarAgent

Attach `ThetaStarAgent` to a GameObject for advanced pathfinding. You can subscribe to path events:

```csharp
using CodenameLib.Pathfinding;

public class MyThetaController : MonoBehaviour
{
    public ThetaStarAgent agent;
    public Transform target;

    void Start()
    {
        agent.OnPathComplete += HandlePathComplete;
        agent.MoveTo(target.position);
    }

    void HandlePathComplete(PathfindingResult result)
    {
        if (result.success)
            Debug.Log("Path found!");
        else
            Debug.LogWarning("Pathfinding failed: " + result.ErrorMessage);
    }
}
```

## API Reference

### `AStarPathfinder2D`
- `PathfindingResult FindPath(Vector3 startWorldPos, Vector3 targetWorldPos, Grid grid, Tilemap[] obstacleTilemaps)`

### `ThetaStarPathfinder`
- `PathfindingResult FindPath2D(Vector3 startWorldPos, Vector3 targetWorldPos, Grid grid, Tilemap[] obstacleTilemaps)`

### `PathfindingAgent2D` & `ThetaStarAgent`
- `void MoveTo(Vector3 targetPosition)`
- `void CalculatePathOnly(Vector3 targetPosition)` (ThetaStarAgent only)

### `PathfindingResult`
- `bool success` — true if a valid path was found
- `List<Vector3> Path` — list of world positions along the path
- `string ErrorMessage` — error details if pathfinding failed

## Visualization

Enable "Draw Gizmos" in the agent scripts to visualize paths and nodes in the Unity Editor.

## Contributing

Feel free to open issues or pull requests for improvements or bug fixes.

## License

Please see the repository for license details.

```
