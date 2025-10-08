# CodenameLib

CodenameLib is a C# library providing robust 2D pathfinding solutions for Unity. It implements both A* and Theta* algorithms, supporting grid-based movement and tilemap obstacles. Designed for game developers and anyone needing efficient navigation on tilemaps, CodenameLib is easy to integrate and extend.

---

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
  - [Quick Start: PathfindingAgent2D](#quick-start-pathfindingagent2d)
  - [Direct API: Using A* or Theta*](#direct-api-using-a-or-theta)
  - [Quick Start: ThetaStarAgent](#quick-start-thetastaragent)
- [API Reference](#api-reference)
  - [AStarPathfinder2D](#astarpathfinder2d)
  - [ThetaStarPathfinder](#thetastarpathfinder)
  - [Pathfinding Agents](#pathfinding-agents)
  - [PathfindingResult](#pathfindingresult)
- [Visualization](#visualization)
- [Extending](#extending)
- [Contributing](#contributing)
- [License](#license)

---

## Features

- **A* and Theta* Algorithms**: Fast, optimal pathfinding for 2D tilemaps.
- **Unity Integration**: Works directly with `Grid` and `Tilemap` components.
- **Ready-to-Use Agents**: Prefab scripts to move objects along paths.
- **Customizable Movement**: Control speed, waypoints, and behavior.
- **Path Visualization**: Gizmo-based rendering of paths and waypoints in the Editor.
- **Event Hooks**: Subscribe to agent events for animation or logic triggers.

---

## Installation

1. **Clone or Download** the repository:
   ```
   git clone https://github.com/taiix/CodenameLib.git
   ```
2. **Copy the Pathfinding Library**: Move everything in `Runtime/Pathfinding/` into your Unity project's `Assets` folder.
3. **Dependencies**: CodenameLib uses Unity's `Grid`, `Tilemap`, and basic `MonoBehaviour` features. No additional packages required.

---

## Usage

### Quick Start: PathfindingAgent2D

Attach `PathfindingAgent2D` to your player or NPC GameObject. Configure these fields in the Inspector:

- `grid`: Reference to your scene's Grid object.
- `obstacleTilemaps`: Array of Tilemaps for obstacles.
- `movementSpeed`, `reachedNodeDistance`, `drawGizmos`: Tweak for behavior and visualization.
- `Player`: The transform to move.
- `Target`: The transform to reach.

**Sample Script:**

```csharp
using CodenameLib.Pathfinding;

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

You can use the core pathfinding classes directly, without any agents.

**A\* Example:**

```csharp
using CodenameLib.Pathfinding;

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
```

**Theta\* Example:**

```csharp
PathfindingResult result = ThetaStarPathfinder.FindPath2D(
    player.transform.position,
    target.transform.position,
    grid,
    obstacleTilemaps
);
```

### Quick Start: ThetaStarAgent

For advanced pathfinding (Theta*), attach `ThetaStarAgent` to your GameObject and use its API:

```csharp
using CodenameLib.Pathfinding;

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

## API Reference

### AStarPathfinder2D

- `PathfindingResult FindPath(Vector3 startWorldPos, Vector3 targetWorldPos, Grid grid, Tilemap[] obstacleTilemaps)`
  - Finds a path from start to target, avoiding obstacles.

### ThetaStarPathfinder

- `PathfindingResult FindPath2D(Vector3 startWorldPos, Vector3 targetWorldPos, Grid grid, Tilemap[] obstacleTilemaps)`
  - Uses Theta* for smoother, more direct paths.

### Pathfinding Agents

- **PathfindingAgent2D** (A*)
  - `void MoveTo(Vector3 targetPosition)` – Moves the assigned player to the given position.
- **ThetaStarAgent** (Theta*)
  - `void MoveTo(Vector3 targetPosition)`
  - `void CalculatePathOnly(Vector3 targetPosition)` – Calculates path, does not move.
  - `event Action<PathfindingResult> OnPathComplete` – Subscribe to get results.
  - `event Action OnMovementStart`, `event Action OnMovementComplete` – Subscribe for movement events.

### PathfindingResult

- `bool success` – True if a path was found.
- `List<Vector3> Path` – World positions for each waypoint.
- `string ErrorMessage` – Details when failing.

---

## Visualization

Enable **Draw Gizmos** in agent scripts to visualize path lines and waypoints in the Unity Editor. Colors indicate:

- Green: Path lines.
- Blue: Path nodes.
- Red: Current target node.

---

## Extending

CodenameLib is designed for easy extension:
- Add new pathfinding algorithms by following the structure of `AStarPathfinder2D` or `ThetaStarPathfinder`.
- Integrate with custom movement or animation systems via agent events.
- Modify cost or heuristic functions for custom movement logic.

---

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request. For larger changes, open an issue to discuss first.

---

## License

This project is licensed under the MIT License. See the [repository](https://github.com/taiix/CodenameLib) for details.

---

## Contact & Support

For questions or support, open an issue on [GitHub](https://github.com/taiix/CodenameLib/issues).
