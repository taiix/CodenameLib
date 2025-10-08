// A* Pathfinding implementation for 2D grid-based maps using Unity's Grid and Tilemap systems.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CodenameLib.Pathfinding
{
    public static class AStarPathfinder2D
    {
        // Directions for movement: 4 cardinal + 4 diagonal
        private static readonly Vector3Int[] Directions = {
            new Vector3Int(-1, 0, 0),  // Left
            new Vector3Int(1, 0, 0),   // Right
            new Vector3Int(0, 1, 0),   // Up
            new Vector3Int(0, -1, 0),  // Down
            new Vector3Int(-1, 1, 0),  // Left Up
            new Vector3Int(1, 1, 0),   // Right Up
            new Vector3Int(-1, -1, 0), // Left Down
            new Vector3Int(1, -1, 0)   // Right Down
        };

        /// <summary>
        /// Finds a path from startWorldPos to targetWorldPos using A* algorithm.
        /// </summary>
        /// <param name="startWorldPos">World position of the start point.</param>
        /// <param name="targetWorldPos">World position of the target point.</param>
        /// <param name="grid">Reference to the Unity Grid.</param>
        /// <param name="obstacleTilemaps">Array of Tilemaps representing obstacles.</param>
        /// <returns>PathfindingResult containing the path or failure info.</returns>
        public static PathfindingResult FindPath(Vector3 startWorldPos, Vector3 targetWorldPos,
            Grid grid, Tilemap[] obstacleTilemaps)
        {
            // Validate input references
            if (grid == null || obstacleTilemaps == null || obstacleTilemaps.Length == 0)
            {
                Debug.LogWarning("AStarPathfinder: Missing grid or obstacle tilemaps reference.");
                return PathfindingResult.Failure();
            }

            // Convert world positions to grid cell positions
            Vector3Int startCell = grid.WorldToCell(startWorldPos);
            Vector3Int targetCell = grid.WorldToCell(targetWorldPos);

            // Early exit if start or target cell is not walkable
            if (!IsWalkable(startCell, obstacleTilemaps) || !IsWalkable(targetCell, obstacleTilemaps))
            {
                return PathfindingResult.Failure();
            }

            // If start and target are the same cell, return immediately
            if (startCell == targetCell)
            {
                return PathfindingResult.Success
                    (new List<Vector3> { grid.GetCellCenterWorld(startCell) });
            }

            // Run A* algorithm to calculate the path
            return CalculatePath(startCell, targetCell, grid, obstacleTilemaps);
        }

        /// <summary>
        /// Core A* pathfinding algorithm implementation.
        /// </summary>
        private static PathfindingResult CalculatePath(Vector3Int startCell, Vector3Int targetCell, Grid grid, Tilemap[] obstacleTilemaps)
        {
            // Open set: cells to be evaluated
            List<Vector3Int> openSet = new List<Vector3Int>();
            // Closed set: cells already evaluated
            HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
            // Map of cell to its parent (for path reconstruction)
            Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            // Cost from start to cell
            Dictionary<Vector3Int, float> gScore = new Dictionary<Vector3Int, float>();
            // Estimated total cost from start to target through cell
            Dictionary<Vector3Int, float> fScore = new Dictionary<Vector3Int, float>();

            // Initialize start node
            openSet.Add(startCell);
            gScore[startCell] = 0;
            fScore[startCell] = HeuristicCostEstimate(startCell, targetCell);

            while (openSet.Count > 0)
            {
                // Find node in openSet with lowest fScore
                Vector3Int current = GetLowestFScoreNode(openSet, fScore);

                // If target reached, reconstruct and return path
                if (current == targetCell)
                {
                    List<Vector3> worldPath = ReconstructPath(cameFrom, current, grid);
                    return PathfindingResult.Success(worldPath);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                // Evaluate all neighbors of current cell
                foreach (Vector3Int direction in Directions)
                {
                    Vector3Int neighbor = current + direction;

                    // Skip if neighbor is already evaluated or not walkable
                    if (closedSet.Contains(neighbor) || !IsWalkable(neighbor, obstacleTilemaps))
                        continue;

                    // Calculate cost from start to neighbor through current
                    float tentativeGScore = gScore[current] + GetMovementCost(current, neighbor);

                    // If neighbor not in openSet, add it
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                    // If this path is not better, skip
                    else if (tentativeGScore >= gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        continue;
                    }

                    // Record best path to neighbor
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + HeuristicCostEstimate(neighbor, targetCell);
                }
            }

            // No path found
            return PathfindingResult.Failure();
        }

        /// <summary>
        /// Returns the node in openSet with the lowest fScore.
        /// </summary>
        private static Vector3Int GetLowestFScoreNode(List<Vector3Int> openSet, Dictionary<Vector3Int, float> fScore)
        {
            Vector3Int lowestNode = openSet[0];
            float lowestScore = fScore.GetValueOrDefault(lowestNode, float.MaxValue);

            for (int i = 1; i < openSet.Count; i++)
            {
                float score = fScore.GetValueOrDefault(openSet[i], float.MaxValue);
                if (score < lowestScore)
                {
                    lowestScore = score;
                    lowestNode = openSet[i];
                }
            }

            return lowestNode;
        }

        /// <summary>
        /// Reconstructs the path from start to target using the cameFrom map.
        /// </summary>
        private static List<Vector3> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current, Grid grid)
        {
            List<Vector3Int> cellPath = new List<Vector3Int> { current };

            // Traverse backwards from target to start
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                cellPath.Add(current);
            }

            cellPath.Reverse();

            // Convert cell positions to world positions
            List<Vector3> worldPath = new List<Vector3>();
            foreach (Vector3Int cell in cellPath)
            {
                worldPath.Add(grid.GetCellCenterWorld(cell));
            }

            return worldPath;
        }

        /// <summary>
        /// Heuristic cost estimate (Manhattan distance) between two cells.
        /// </summary>
        private static float HeuristicCostEstimate(Vector3Int from, Vector3Int to)
        {
            // Manhattan distance for grid-based pathfinding
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        /// <summary>
        /// Returns movement cost between two adjacent cells.
        /// Diagonal movement costs more than cardinal movement.
        /// </summary>
        private static float GetMovementCost(Vector3Int from, Vector3Int to)
        {
            // Diagonal movement costs more
            bool isDiagonal = (from.x != to.x) && (from.y != to.y);
            return isDiagonal ? 1.414f : 1f; // √2 for diagonal
        }

        /// <summary>
        /// Checks if a cell is walkable (not blocked by any obstacle tilemap).
        /// </summary>
        public static bool IsWalkable(Vector3Int cellPosition, Tilemap[] obstacleTilemaps)
        {
            foreach (Tilemap obstacleTilemap in obstacleTilemaps)
            {
                if (obstacleTilemap != null && obstacleTilemap.HasTile(cellPosition))
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Result of a pathfinding operation.
    /// </summary>
    public struct PathfindingResult
    {
        public bool success { get; }
        public List<Vector3> Path { get; }
        public string ErrorMessage { get; }

        private PathfindingResult(bool success, List<Vector3> path, string errorMessage)
        {
            this.success = success;
            Path = path;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Returns a successful result with the found path.
        /// </summary>
        public static PathfindingResult Success(List<Vector3> path)
        {
            return new PathfindingResult(true, path, null);
        }

        /// <summary>
        /// Returns a failure result with an error message.
        /// </summary>
        public static PathfindingResult Failure(string errorMessage = "No path found")
        {
            return new PathfindingResult(false, null, errorMessage);
        }
    }
}