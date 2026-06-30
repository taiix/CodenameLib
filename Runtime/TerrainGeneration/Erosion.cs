using UnityEngine;

namespace CodenameLib.ProceduralTerrain
{
    public static class Erosion
    {
        public class Droplet
        {
            public Vector2 position;
            public Vector2 direction;   //normalized
            public float speed = 1f;
            public float water = 1f;
            public float sediment = 0f;
            public Vector2 velocity; // currently unused
            public float sedimentCapacityFactor = 4f;
            public float minSedimentCapacity = 0.01f;
            public float depositionSpeed = 0.3f;
            public float erosionSpeed = 0.3f;
        }

        public struct HeightAndGradient
        {
            public float height;
            public float gradientX;
            public float gradientY;
        }

        public static void ErosionS(float[,] heightMap, Droplet droplet)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            for (int steps = 0; steps < 30; steps++)
            {
                // Store OLD position and height
                Vector2 oldPos = droplet.position;
                HeightAndGradient oldG = GradientCalculation(heightMap, oldPos.x, oldPos.y);

                // Move droplet
                droplet.direction = Movement(heightMap, oldPos, droplet.direction, 0.05f);
                droplet.position += droplet.direction * droplet.speed;

                // Check bounds
                if (IsOutOfBounds(heightMap, droplet.position))
                    break;

                // Get NEW height
                HeightAndGradient newG = GradientCalculation(heightMap, droplet.position.x, droplet.position.y);

                // Height difference = OLD - NEW (positive => downhill)
                float deltaHeight = oldG.height - newG.height;

                // Sediment capacity increases with downhill slope, speed, and water
                float downhill = Mathf.Max(deltaHeight, 0f);
                float sedimentCapacity = Mathf.Max(
                    downhill * droplet.speed * droplet.water * droplet.sedimentCapacityFactor,
                    droplet.minSedimentCapacity
                );

                // Erode or deposit
                if (droplet.sediment > sedimentCapacity || deltaHeight < 0f)
                {
                    // Deposit when carrying too much or going uphill
                    float amountToDrop = (deltaHeight < 0f)
                        ? Mathf.Min(droplet.sediment, -deltaHeight) * droplet.depositionSpeed
                        : (droplet.sediment - sedimentCapacity) * droplet.depositionSpeed;

                    amountToDrop = Mathf.Max(0f, Mathf.Min(amountToDrop, droplet.sediment));
                    if (amountToDrop > 0f)
                    {
                        DepositSediment(heightMap, oldPos, amountToDrop, 3);
                        droplet.sediment -= amountToDrop;
                    }
                }
                else
                {
                    // Erode when moving downhill and under capacity
                    float amountToErode = Mathf.Min(sedimentCapacity - droplet.sediment, downhill) * droplet.erosionSpeed;
                    amountToErode = Mathf.Max(0f, amountToErode);
                    if (amountToErode > 0f)
                    {
                        ErodeTerrain(heightMap, oldPos, amountToErode, 3);
                        droplet.sediment += amountToErode;
                    }
                }

                // Update physics
                droplet.speed = CalculateNewSpeed(droplet.speed, deltaHeight);
                droplet.water *= 0.99f; // Evaporation

                // Stop if out of water
                if (droplet.water < 0.01f) break;
            }
        }

        // Fixed movement function
        public static Vector2 Movement(float[,] heightMap, Vector2 currentPos, Vector2 currentDir, float inertia)
        {
            HeightAndGradient g = GradientCalculation(heightMap, currentPos.x, currentPos.y);

            Vector2 downhillDir = new Vector2(-g.gradientX, -g.gradientY);

            if (downhillDir.sqrMagnitude > 0.0001f)
                downhillDir.Normalize();

            Vector2 newDir = currentDir * inertia + downhillDir * (1 - inertia);

            if (newDir.sqrMagnitude > 0.0001f)
                newDir.Normalize();

            return newDir;
        }

        // Speed calculation
        private static float CalculateNewSpeed(float currentSpeed, float heightDifference)
        {
            if (heightDifference > 0)
            {
                // Moving downhill - accelerate
                return Mathf.Sqrt(currentSpeed * currentSpeed + heightDifference * 4f);
            }
            else
            {
                // Moving uphill - slow down
                return currentSpeed * 0.9f;
            }
        }

        // Simple brush implementation
        private static void ErodeTerrain(float[,] heightMap, Vector2 position, float amount, int radius)
        {
            ApplyBrush(heightMap, position, amount, radius, true);
        }

        private static void DepositSediment(float[,] heightMap, Vector2 position, float amount, int radius)
        {
            ApplyBrush(heightMap, position, amount, radius, false);
        }

        private static void ApplyBrush(float[,] heightMap, Vector2 center, float totalAmount, int radius, bool isErosion)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            int centerX = (int)center.x;
            int centerY = (int)center.y;

            float totalWeight = 0f;
            int r2 = radius * radius;

            // First pass: calculate total weight (in-bounds only)
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    int targetX = centerX + x;
                    int targetY = centerY + y;

                    if (targetX < 0 || targetX >= width || targetY < 0 || targetY >= height)
                        continue;

                    int d2 = x * x + y * y;
                    if (d2 <= r2)
                    {
                        float weight = 1f - Mathf.Sqrt(d2) / radius;
                        totalWeight += weight;
                    }
                }
            }

            if (totalWeight <= 0f || totalAmount == 0f)
                return;

            // Second pass: apply changes
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    int targetX = centerX + x;
                    int targetY = centerY + y;

                    if (targetX < 0 || targetX >= width || targetY < 0 || targetY >= height)
                        continue;

                    int d2 = x * x + y * y;
                    if (d2 <= r2)
                    {
                        float weight = (1f - Mathf.Sqrt(d2) / radius) / totalWeight;
                        float amount = totalAmount * weight;

                        if (isErosion)
                        {
                            heightMap[targetX, targetY] = Mathf.Max(0f, heightMap[targetX, targetY] - amount);
                        }
                        else
                        {
                            heightMap[targetX, targetY] += amount;
                        }
                    }
                }
            }
        }

        private static bool IsOutOfBounds(float[,] heightMap, Vector2 position)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            return position.x < 1 || position.x >= width - 1 || position.y < 1 || position.y >= height - 1;
        }

        public static HeightAndGradient GradientCalculation(float[,] heightMap, float posX, float posY)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            int coordX = Mathf.Clamp((int)posX, 0, width - 2);
            int coordY = Mathf.Clamp((int)posY, 0, height - 2);

            float fracX = posX - coordX;
            float fracY = posY - coordY;

            float heightBL = heightMap[coordX, coordY];         // Bottom-left
            float heightBR = heightMap[coordX + 1, coordY];     // Bottom-right
            float heightTL = heightMap[coordX, coordY + 1];     // Top-left
            float heightTR = heightMap[coordX + 1, coordY + 1]; // Top-right

            float gradientX = (heightBR - heightBL) * (1 - fracY) + (heightTR - heightTL) * fracY;
            float gradientY = (heightTL - heightBL) * (1 - fracX) + (heightTR - heightBR) * fracX;

            float heightResult =
                heightBL * (1 - fracX) * (1 - fracY) +
                heightBR * fracX * (1 - fracY) +
                heightTL * (1 - fracX) * fracY +
                heightTR * fracX * fracY;

            return new HeightAndGradient
            {
                height = heightResult,
                gradientX = gradientX,
                gradientY = gradientY
            };
        }
    }
}
