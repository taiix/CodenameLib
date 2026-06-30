using System.Collections.Generic;
using UnityEngine;

namespace CodenameLib.ProceduralTerrain
{
    // GPU hydraulic (droplet) erosion. Reshapes a normalized heightmap in place
    // by dispatching the "Erode" kernel in TerrainCompute.compute.
    //
    // This is the GPU counterpart to the CPU Erosion class. It lives inside the
    // CodenameDevLib assembly (not Assets/AI) because it is called by
    // MeshTerrainGenerator and uses TerrainSettings/ErosionSettings; splitting it
    // into another assembly would create a circular reference.
    public static class GpuErosion
    {
        private static ComputeShader _compute;
        private static int _kernel = -1;

        static GpuErosion()
        {
            _compute = Resources.Load<ComputeShader>("ComputeShaders/TerrainCompute");
            if (_compute != null)
                _kernel = _compute.FindKernel("Erode");
            else
                Debug.LogError("[GpuErosion] ⚠️ TerrainCompute shader not found.");
        }

        /// <summary>
        /// Erodes <paramref name="heightmap"/> ([mapSize, mapSize], values ~0..1) in place.
        /// </summary>
        public static void Erode(float[,] heightmap, ErosionSettings s)
        {
            if (!s.enabled || s.iterations <= 0)
                return;

            if (_compute == null || _kernel < 0)
            {
                Debug.LogWarning("[GpuErosion] Compute shader unavailable; skipping erosion.");
                return;
            }

            int mapSize = heightmap.GetLength(0);
            int n = mapSize * mapSize;

            // Flatten [x, y] -> x + y * mapSize (matches the kernel's indexing).
            float[] map = new float[n];
            for (int y = 0; y < mapSize; y++)
                for (int x = 0; x < mapSize; x++)
                    map[x + y * mapSize] = heightmap[x, y];

            int radius = Mathf.Max(1, s.erosionRadius);
            int borderSize = radius + 1;

            // Build the erosion brush: relative 1D offsets + normalized falloff weights.
            List<int> brushIndexList = new List<int>();
            List<float> brushWeightList = new List<float>();
            float weightSum = 0f;
            for (int by = -radius; by <= radius; by++)
            {
                for (int bx = -radius; bx <= radius; bx++)
                {
                    float sqrDst = bx * bx + by * by;
                    if (sqrDst < radius * radius)
                    {
                        float weight = 1f - Mathf.Sqrt(sqrDst) / radius;
                        weightSum += weight;
                        brushIndexList.Add(by * mapSize + bx);
                        brushWeightList.Add(weight);
                    }
                }
            }
            for (int i = 0; i < brushWeightList.Count; i++)
                brushWeightList[i] /= weightSum;
            int brushLength = brushIndexList.Count;

            // Random droplet start cells, kept inside the border so the brush stays in range.
            System.Random prng = new System.Random(s.seed);
            int lo = borderSize;
            int hi = mapSize - borderSize; // System.Random.Next upper bound is exclusive
            int[] randomIndices = new int[s.iterations];
            for (int i = 0; i < s.iterations; i++)
            {
                int rx = prng.Next(lo, hi);
                int ry = prng.Next(lo, hi);
                randomIndices[i] = ry * mapSize + rx;
            }

            ComputeBuffer mapBuffer = new ComputeBuffer(n, sizeof(float));
            ComputeBuffer randomIndexBuffer = new ComputeBuffer(s.iterations, sizeof(int));
            ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushLength, sizeof(int));
            ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushLength, sizeof(float));

            try
            {
                mapBuffer.SetData(map);
                randomIndexBuffer.SetData(randomIndices);
                brushIndexBuffer.SetData(brushIndexList.ToArray());
                brushWeightBuffer.SetData(brushWeightList.ToArray());

                _compute.SetBuffer(_kernel, "ErosionMap", mapBuffer);
                _compute.SetBuffer(_kernel, "randomIndices", randomIndexBuffer);
                _compute.SetBuffer(_kernel, "brushIndices", brushIndexBuffer);
                _compute.SetBuffer(_kernel, "brushWeights", brushWeightBuffer);

                _compute.SetInt("numDroplets", s.iterations);
                _compute.SetInt("mapSize", mapSize);
                _compute.SetInt("brushLength", brushLength);
                _compute.SetInt("borderSize", borderSize);
                _compute.SetInt("maxLifetime", s.maxDropletLifetime);
                _compute.SetFloat("inertia", s.inertia);
                _compute.SetFloat("sedimentCapacityFactor", s.sedimentCapacityFactor);
                _compute.SetFloat("minSedimentCapacity", s.minSedimentCapacity);
                _compute.SetFloat("depositSpeed", s.depositSpeed);
                _compute.SetFloat("erodeSpeed", s.erodeSpeed);
                _compute.SetFloat("evaporateSpeed", s.evaporateSpeed);
                _compute.SetFloat("gravity", s.gravity);
                _compute.SetFloat("startSpeed", s.startSpeed);
                _compute.SetFloat("startWater", s.startWater);

                int threadGroups = Mathf.CeilToInt(s.iterations / 64f);
                _compute.Dispatch(_kernel, threadGroups, 1, 1);

                mapBuffer.GetData(map);
            }
            finally
            {
                mapBuffer.Release();
                randomIndexBuffer.Release();
                brushIndexBuffer.Release();
                brushWeightBuffer.Release();
            }

            // Write the eroded heights back into the [x, y] array.
            for (int y = 0; y < mapSize; y++)
                for (int x = 0; x < mapSize; x++)
                    heightmap[x, y] = map[x + y * mapSize];
        }
    }
}
