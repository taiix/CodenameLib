using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CodenameLib.Pathfinding
{
    public class PathfindingAgent2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap[] obstacleTilemaps;

        [Header("Pathfinding Settings")]
        [SerializeField] private float movementSpeed = 5f;
        [SerializeField] private float reachedNodeDistance = 0.1f;
        [SerializeField] private bool drawGizmos = true;

        [Header("Runtime Info")]
        [SerializeField] private List<Vector3> currentPath = new List<Vector3>();
        [SerializeField] private bool isMoving = false;
        [SerializeField] private int currentPathIndex = 0;

        private Coroutine movementCoroutine;
        public Transform Player;
        public Transform Target;
        
        private void Start()
        {
            MoveTo(Target.position);
        }

        public void MoveTo(Vector3 targetPosition)
        {
            StopAllMovement();

            PathfindingResult result = AStarPathfinder2D.FindPath(Player.transform.position, targetPosition, grid, obstacleTilemaps);

            if (result.success)
            {
                currentPath = result.Path;
                movementCoroutine = StartCoroutine(FollowPath());
            }
            else
            {
                Debug.LogWarning($"Pathfinding failed: {result.ErrorMessage}");
            }
        }

        private void StopAllMovement()
        {
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }
            isMoving = false;
        }

        private IEnumerator FollowPath()
        {
            if (currentPath == null || currentPath.Count == 0)
                yield break;

            isMoving = true;
            currentPathIndex = 0;

            while (currentPathIndex < currentPath.Count)
            {
                Vector3 targetPosition = currentPath[currentPathIndex];

                Player.transform.position = Vector3.MoveTowards(Player.transform.position, targetPosition, movementSpeed * Time.deltaTime);

                if (Vector3.Distance(Player.transform.position, targetPosition) <= reachedNodeDistance)
                {
                    currentPathIndex++;
                }

                yield return null;
            }

            isMoving = false;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || currentPath == null || currentPath.Count == 0)
                return;

            Gizmos.color = Color.green;

            // Draw path lines
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }

            // Draw path nodes
            Gizmos.color = Color.blue;
            foreach (Vector3 point in currentPath)
            {
                Gizmos.DrawSphere(point, 0.1f);
            }

            // Draw current target
            if (isMoving && currentPathIndex < currentPath.Count)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(currentPath[currentPathIndex], 0.15f);
            }
        }
    }
}