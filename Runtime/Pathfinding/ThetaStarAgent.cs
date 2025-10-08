using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CodenameLib.Pathfinding
{
    public class ThetaStarAgent : MonoBehaviour
    {
        [Header("2D References")]
        [SerializeField] private Grid grid2D;
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

        public event System.Action<PathfindingResult> OnPathComplete;
        public event System.Action OnMovementStart;
        public event System.Action OnMovementComplete;

        public void MoveTo(Vector3 targetPosition)
        {
            StopAllMovement();

            PathfindingResult result;


            result = ThetaStarPathfinder.FindPath2D(transform.position, targetPosition, grid2D, obstacleTilemaps);


            if (result.success)
            {
                currentPath = result.Path;
                movementCoroutine = StartCoroutine(FollowPath());
                OnPathComplete?.Invoke(result);
            }
            else
            {
                Debug.LogWarning($"Theta* Pathfinding failed: {result.ErrorMessage}");
                OnPathComplete?.Invoke(result);
            }
        }

        public void CalculatePathOnly(Vector3 targetPosition)
        {
            PathfindingResult result;

            
                result = ThetaStarPathfinder.FindPath2D(transform.position, targetPosition, grid2D, obstacleTilemaps);
            
            

            OnPathComplete?.Invoke(result);
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
            OnMovementStart?.Invoke();

            while (currentPathIndex < currentPath.Count)
            {
                Vector3 targetPosition = currentPath[currentPathIndex];

                // Move towards current waypoint
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);

              

                // Check if reached current waypoint
                if (Vector3.Distance(transform.position, targetPosition) <= reachedNodeDistance)
                {
                    currentPathIndex++;
                }

                yield return null;
            }

            isMoving = false;
            OnMovementComplete?.Invoke();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || currentPath == null || currentPath.Count == 0)
                return;

            // Different colors for 2D vs 3D

            // Draw path lines
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }

            // Draw path nodes
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