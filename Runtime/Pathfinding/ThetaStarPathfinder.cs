using System.Collections.Generic;
using UnityEngine;

namespace CodenameLib.Pathfinding
{
    public static class ThetaStarPathfinder
    {
        // 2D directions (for top-down 2D games)
        private static readonly Vector3Int[] Directions2D = {
            new Vector3Int(-1, 0, 0),  // Left
            new Vector3Int(1, 0, 0),   // Right
            new Vector3Int(0, 1, 0),   // Up
            new Vector3Int(0, -1, 0),  // Down
            new Vector3Int(-1, 1, 0),  // Left Up
            new Vector3Int(1, 1, 0),   // Right Up
            new Vector3Int(-1, -1, 0), // Left Down
            new Vector3Int(1, -1, 0)   // Right Down
        };



        #region 2D Pathfinding (Tilemap-based)

        public static PathfindingResult FindPath2D(Vector3 startWorldPos, Vector3 targetWorldPos,
            Grid grid, UnityEngine.Tilemaps.Tilemap[] obstacleTilemaps)
        {
            if (grid == null || obstacleTilemaps == null || obstacleTilemaps.Length == 0)
            {
                return PathfindingResult.Failure("Missing grid or obstacle tilemaps reference.");
            }

            Vector3Int startCell = grid.WorldToCell(startWorldPos);
            Vector3Int targetCell = grid.WorldToCell(targetWorldPos);

            // Ensure we're working in 2D (z=0)
            startCell.z = 0;
            targetCell.z = 0;

            if (!IsWalkable2D(startCell, obstacleTilemaps) || !IsWalkable2D(targetCell, obstacleTilemaps))
            {
                return PathfindingResult.Failure("Start or target position is not walkable.");
            }

            if (startCell == targetCell)
            {
                return PathfindingResult.Success(new List<Vector3> { grid.GetCellCenterWorld(startCell) });
            }

            return CalculateThetaStarPath(startCell, targetCell, grid, obstacleTilemaps);
        }

        private static bool IsWalkable2D(Vector3Int cellPosition, UnityEngine.Tilemaps.Tilemap[] obstacleTilemaps)
        {
            foreach (var obstacleTilemap in obstacleTilemaps)
            {
                if (obstacleTilemap != null && obstacleTilemap.HasTile(cellPosition))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        private static PathfindingResult CalculateThetaStarPath(
            Vector3Int startCell, Vector3Int targetCell,
            Grid grid, UnityEngine.Tilemaps.Tilemap[] obstacleTilemaps)
        {
            List<Vector3Int> openSet = new List<Vector3Int>();
            HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
            Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            Dictionary<Vector3Int, float> gScore = new Dictionary<Vector3Int, float>();
            Dictionary<Vector3Int, float> fScore = new Dictionary<Vector3Int, float>();

            // Initialize start node
            openSet.Add(startCell);
            cameFrom[startCell] = startCell;
            gScore[startCell] = 0;
            fScore[startCell] = Heuristic(startCell, targetCell);

            while (openSet.Count > 0)
            {
                Vector3Int current = GetLowestFScoreNode(openSet, fScore);

                if (current == targetCell)
                {
                    List<Vector3> worldPath = ReconstructPath(cameFrom, current, grid);
                    return PathfindingResult.Success(worldPath);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (Vector3Int direction in Directions2D)
                {
                    Vector3Int neighbor = current + direction;

                    // Skip if neighbor is in closed set or not walkable
                    if (closedSet.Contains(neighbor) || !IsWalkable2D(neighbor, obstacleTilemaps))
                        continue;

                    // Theta* Core: Check line-of-sight to parent's parent
                    Vector3Int parentOfCurrent = cameFrom[current];
                    Vector3Int potentialParent;
                    float tentativeGScore;

                    if (parentOfCurrent != current && HasLineOfSight2D(parentOfCurrent, neighbor, grid, obstacleTilemaps))
                    {
                        // Path through grandparent (Theta* optimization)
                        potentialParent = parentOfCurrent;
                        tentativeGScore = gScore[parentOfCurrent] + Heuristic(parentOfCurrent, neighbor);
                    }
                    else
                    {
                        // Standard path through current node
                        potentialParent = current;
                        tentativeGScore = gScore[current] + GetMovementCost(current, neighbor);
                    }

                    bool isNewNode = !openSet.Contains(neighbor);

                    if (isNewNode || tentativeGScore < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        cameFrom[neighbor] = potentialParent;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + Heuristic(neighbor, targetCell);

                        if (isNewNode)
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return PathfindingResult.Failure("No path exists between start and target positions.");
        }

        private static Vector3Int GetLowestFScoreNode
            (List<Vector3Int> openSet, Dictionary<Vector3Int, float> fScore)
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

        private static List<Vector3> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current,
            Grid grid)
        {
            List<Vector3Int> cellPath = new List<Vector3Int> { current };

            while (cameFrom.ContainsKey(current) && current != cameFrom[current])
            {
                current = cameFrom[current];
                cellPath.Add(current);
            }

            cellPath.Reverse();

            List<Vector3> worldPath = new List<Vector3>();
            foreach (Vector3Int cell in cellPath)
            {
                worldPath.Add(grid.GetCellCenterWorld(cell));
            }

            return worldPath;
        }

        private static float Heuristic(Vector3Int from, Vector3Int to)
        {

            return Mathf.Sqrt(Mathf.Pow(from.x - to.x, 2) + Mathf.Pow(from.y - to.y, 2));

        }

        private static float GetMovementCost(Vector3Int from, Vector3Int to)
        {

            bool isDiagonal = (from.x != to.x) && (from.y != to.y);
            return isDiagonal ? 1.414f : 1f;

        }

        private static bool HasLineOfSight2D(Vector3Int start, Vector3Int end, Grid grid, UnityEngine.Tilemaps.Tilemap[] obstacleTilemaps)
        {
            return BresenhamLineOfSight2D(start, end, obstacleTilemaps);
        }

        private static bool BresenhamLineOfSight2D(Vector3Int start, Vector3Int end, UnityEngine.Tilemaps.Tilemap[] obstacleTilemaps)
        {
            int x0 = start.x;
            int y0 = start.y;
            int x1 = end.x;
            int y1 = end.y;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);

            int sx = (x0 < x1) ? 1 : -1;
            int sy = (y0 < y1) ? 1 : -1;

            int err = dx - dy;

            int currentX = x0;
            int currentY = y0;

            while (true)
            {
                if (!IsWalkable2D(new Vector3Int(currentX, currentY, 0), obstacleTilemaps))
                    return false;

                if (currentX == x1 && currentY == y1)
                    return true;

                int e2 = 2 * err;

                if (e2 > -dy)
                {
                    err -= dy;
                    currentX += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    currentY += sy;
                }
            }
        }
    }
}