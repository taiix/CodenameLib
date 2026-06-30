# Reusable Terrain Layer Shader

A height + slope driven, triplanar-mapped terrain material system for URP.
Stack one `TerrainLayer` node per material (sand, grass, dirt, rock, snow…),
give each a height band and a slope band, and the main graph blends them.

## Files

| File | Role |
|------|------|
| `TerrainLayers.hlsl` | The reusable logic: triplanar albedo, whiteout triplanar normals, height×slope weight. Entry point `TerrainLayer_float`. |
| `TerrainLayer.shadersubgraph` | Wraps the HLSL in a Custom Function node and exposes per-layer inputs. This is the node you stack. |
| `TerrainShader.shadergraph` | Main material graph — stack `TerrainLayer` nodes here and normalize. |

## TerrainLayer node — inputs / outputs

**Inputs**
- `Height` (Float) — elevation used for the height band. Feed a 0..1 value (see "Feeding Height").
- `Albedo`, `NormalMap` (Texture2D)
- `Tint` (Color), `Smoothness`, `Tiling`, `NormalStrength`
- `HeightMin`, `HeightMax`, `HeightBlend` — elevation band + soft-edge width
- `SlopeMin`, `SlopeMax`, `SlopeBlend` — steepness band (0 = flat, 1 = vertical) + soft-edge width
- `TriplanarSharpness` — how hard the triplanar projection switches between axes (≈4)

**Outputs**
- `Color` (Vector3), `Normal` (world-space Vector3), `Smoothness` (Float), `Weight` (Float, 0..1)

`WorldPos` (absolute world) and `WorldNormal` (world) are read internally; you don't wire them.

## Main graph: stacking pattern

For N layers, accumulate weighted contributions then divide by total weight:

```
sumColor  = Σ (Layer_i.Color  * Layer_i.Weight)
sumNormal = Σ (Layer_i.Normal * Layer_i.Weight)
sumSmooth = Σ (Layer_i.Smoothness * Layer_i.Weight)
sumW      = Σ  Layer_i.Weight

BaseColor       = sumColor  / max(sumW, 1e-4)
Normal (World)  = normalize(sumNormal / max(sumW, 1e-4))
Smoothness      = sumSmooth / max(sumW, 1e-4)
```

Node-by-node for each layer:
1. `Multiply` Color × Weight → add into a running `Add` chain for color.
2. `Multiply` Normal × Weight → running `Add` chain for normal.
3. `Multiply` Smoothness × Weight → running `Add` chain for smoothness.
4. `Add` Weight into a running `Add` chain for total weight.

Then once:
5. `Divide` each summed channel by the total weight (wrap total weight in `Max(w, 0.0001)` to avoid divide-by-zero where no layer claims a pixel).
6. Feed color → **Base Color**, normal → **Normal** block (set the block's space to **World**), smoothness → **Smoothness**.

> Set the **Normal** block space to *World* (Graph Settings / block dropdown) because the layer outputs world-space normals. If you'd rather keep the default Tangent-space Normal block, add a `Transform (World → Tangent)` node before it.

## Feeding `Height`

The band is evaluated against whatever you pass as `Height`, so make it 0..1:

- **From world position:** `Position(Absolute World).y` → `Remap` from `[minWorldY, maxWorldY]` to `[0,1]` → `Saturate`. Drive `minWorldY/maxWorldY` from the terrain's `heightMultiplier`.
- **From the existing heightmap:** sample `_HeightMap` (the texture the compute shader fills) by UV and use `.r`. This matches the CPU mesh displacement exactly and is the most consistent option for this project.

Wire that single Height value into every layer's `Height` input.

## Suggested layer setup (5 layers)

| Layer | HeightMin | HeightMax | SlopeMin | SlopeMax | Notes |
|-------|-----------|-----------|----------|----------|-------|
| Sand  | 0.00 | 0.25 | 0.0 | 0.4 | low, fairly flat (beaches) |
| Grass | 0.20 | 0.55 | 0.0 | 0.35 | mid, flat only |
| Dirt  | 0.45 | 0.75 | 0.0 | 0.6 | mid-high, gentle slopes |
| Rock  | 0.00 | 1.00 | 0.35 | 1.0 | **any height, steep faces** — cliffs |
| Snow  | 0.75 | 1.00 | 0.0 | 0.45 | high + flat-ish |

`HeightBlend ≈ 0.05`, `SlopeBlend ≈ 0.1` give soft transitions; raise for softer.
Rock spanning all heights but only steep slopes is what makes cliffs read correctly.
Because the main graph normalizes by total weight, overlapping bands cross-fade and
gaps (no layer) fall back gracefully instead of going black.

## If the subgraph needs rebuilding in-editor

The `.shadersubgraph` was authored by hand. If Unity flags it, recreate it:
1. Create → Shader Graph → Sub Graph. Add a **Custom Function** node.
2. Set **Type = File**, **Name = `TerrainLayer`**, **Source = `TerrainLayers.hlsl`**.
3. Add inputs in this exact order (the HLSL binds positionally):
   `WorldPos(Vector3)`, `WorldNormal(Vector3)`, `Height(Float)`, `Albedo(Texture2D)`,
   `NormalMap(Texture2D)`, `SS(SamplerState)`, `Tint(Vector4)`, `Tiling(Float)`,
   `NormalStrength(Float)`, `Smoothness(Float)`, `HeightMin/Max/Blend(Float)`,
   `SlopeMin/Max/Blend(Float)`, `TriplanarSharpness(Float)`.
4. Add outputs in order: `OutColor(Vector3)`, `OutNormal(Vector3)`, `OutSmoothness(Float)`, `OutWeight(Float)`.
5. Inside the subgraph feed `WorldPos` from a **Position (Absolute World)** node and
   `WorldNormal` from a **Normal Vector (World)** node; expose everything else as
   blackboard properties; leave `SS` unconnected (default sampler).
6. Route the four outputs to the Sub Graph **Output** node as
   `Color, Normal, Smoothness, Weight`.
