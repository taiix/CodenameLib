using CodenameLib.Pathfinding;
using UnityEngine.Tilemaps;
using UnityEngine;

public class Enemy2D : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap[] obstacles;
    [SerializeField] private Transform player;
    [SerializeField] private Transform target;
    [SerializeField] private ThetaStarAgent agent;

    private void Start()
    {
        ChasePlayer2D();
    }
    void ChasePlayer2D()
    {
        // Simple 2D pathfinding
        PathfindingResult result = ThetaStarPathfinder.FindPath2D(
            transform.position,
            player.position,
            grid,
            obstacles
        );

        // Or use the agent component (set to 2D mode in inspector)
        agent.MoveTo(target.position);
    }
}