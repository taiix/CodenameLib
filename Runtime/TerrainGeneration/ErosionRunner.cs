using System;
using UnityEngine;
using CodenameLib.ProceduralTerrain;

public static class ErosionRunner
{
    // Spawns N droplets and applies erosion in-place on the given heightMap.
    // Use `configure` to tweak droplet parameters per spawn (optional).
    public static void Apply(float[,] heightMap, int dropletCount, int seed = 1337, Action<CodenameLib.ProceduralTerrain.Erosion.Droplet> configure = null)
    {
        if (heightMap == null) return;

        var rng = new System.Random(seed);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        for (int i = 0; i < dropletCount; i++)
        {
            var d = new CodenameLib.ProceduralTerrain.Erosion.Droplet
            {
                // Start away from borders to avoid early exits
                position = new Vector2(
                    1f + (float)rng.NextDouble() * (width - 2f),
                    1f + (float)rng.NextDouble() * (height - 2f)
                ),
                // Zero direction lets the gradient fully guide the first step
                direction = Vector2.zero,
                speed = 1f,
                water = 1f,
                sediment = 0f
            };

            // Optional per-droplet tuning
            configure?.Invoke(d);

            CodenameLib.ProceduralTerrain.Erosion.ErosionS(heightMap, d);
        }
    }
}
