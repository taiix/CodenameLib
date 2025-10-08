## Creating Your Own Pathfinding Agent

If you want more control or custom behavior (for example, unique movement systems, event hooks, or integration into your own GameObjects), you can build your own agent using the core pathfinding algorithms provided by CodenameLib.

### Steps to Create a Custom Agent

#### 1. Reference the Pathfinding Namespace

At the top of your script, import the library:

```csharp
using CodenameLib.Pathfinding;
```

#### 2. Prepare Required Components

- **Grid**: Reference to your Unity Grid object.
- **Tilemap[] obstacleTilemaps**: Array of Tilemaps which mark obstacles.
- **Transform**: Your agent's transform.
- **Target Position**: The destination world position.

#### 3. Calculate the Path

Use the static methods provided by the library to generate a path:

```csharp
PathfindingResult result = AStarPathfinder2D.FindPath(
    transform.position, targetPosition, grid, obstacleTilemaps
);

// Or, for Theta*:
PathfindingResult result = ThetaStarPathfinder.FindPath2D(
    transform.position, targetPosition, grid, obstacleTilemaps
);
```

#### 4. Process the Path

Check if a path was found, then use the waypoints in your custom movement logic:

```csharp
if (result.success)
{
    List<Vector3> path = result.Path;
    // Implement your movement along the path
}
else
{
    Debug.LogWarning("Pathfinding failed: " + result.ErrorMessage);
}
```

#### 5. Move the Agent

You can write your own coroutine, update method, or movement logic to follow the path:

```csharp
IEnumerator MoveAlongPath(List<Vector3> path, float speed)
{
    foreach (var waypoint in path)
    {
        while (Vector3.Distance(transform.position, waypoint) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, waypoint, speed * Time.deltaTime);
            yield return null;
        }
    }
}
```

To start movement:

```csharp
if (result.success)
    StartCoroutine(MoveAlongPath(result.Path, movementSpeed));
```

#### 6. Example Script

Here’s a complete example of a custom agent using A*:

```csharp
using CodenameLib.Pathfinding;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MyCustomAgent : MonoBehaviour
{
    public Grid grid;
    public Tilemap[] obstacleTilemaps;
    public float movementSpeed = 5f;

    public void MoveTo(Vector3 targetPosition)
    {
        PathfindingResult result = AStarPathfinder2D.FindPath(
            transform.position, targetPosition, grid, obstacleTilemaps
        );

        if (result.success)
            StartCoroutine(MoveAlongPath(result.Path, movementSpeed));
        else
            Debug.LogWarning(result.ErrorMessage);
    }

    IEnumerator MoveAlongPath(List<Vector3> path, float speed)
    {
        foreach (var waypoint in path)
        {
            while (Vector3.Distance(transform.position, waypoint) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, waypoint, speed * Time.deltaTime);
                yield return null;
            }
        }
    }
}
```

#### 7. Customize Further

- Add event callbacks, animation, or custom logic.
- Integrate with AI, navigation, or game state systems.
- Visualize the path using Gizmos or other Unity features.

---

**Tip:**  
By using the core algorithms, your agent can take full advantage of CodenameLib's pathfinding while retaining maximum flexibility for unique behaviors!
