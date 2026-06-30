#ifndef CODENAME_TERRAIN_LAYERS_INCLUDED
#define CODENAME_TERRAIN_LAYERS_INCLUDED

// =====================================================================
// CodenameLib - Reusable terrain layer logic (triplanar + height/slope)
// Used by the "TerrainLayer" sub graph via a Custom Function node
// (file mode, function = "TerrainLayer").
//
// Each layer produces a Color / Normal / Smoothness contribution and a
// Weight in [0,1]. The main graph accumulates weighted contributions and
// divides by the summed weight, so any number of stacked layers blend
// without leaving black gaps.
// =====================================================================

// Soft "plateau" band: ~1 between [minV, maxV], fading to 0 over `blend`
// on each side. Works for both the height range and the slope range.
float Terrain_BandWeight(float x, float minV, float maxV, float blend)
{
    blend = max(blend, 1e-4);
    float lower = smoothstep(minV - blend, minV + blend, x);
    float upper = 1.0 - smoothstep(maxV - blend, maxV + blend, x);
    return saturate(lower * upper);
}

// Per-axis projection weights for triplanar mapping, from a world normal.
float3 Terrain_TriplanarBlend(float3 worldNormal, float sharpness)
{
    float3 n = pow(abs(worldNormal), max(sharpness, 1e-4));
    return n / max(n.x + n.y + n.z, 1e-4);
}

// Triplanar albedo: sample on the 3 world planes and blend.
float4 Terrain_TriplanarColor(UnityTexture2D tex, UnitySamplerState ss,
                              float3 worldPos, float3 blend, float scale)
{
    float2 uvX = worldPos.zy * scale; // projected along X
    float2 uvY = worldPos.xz * scale; // projected along Y (top-down)
    float2 uvZ = worldPos.xy * scale; // projected along Z

    float4 cx = SAMPLE_TEXTURE2D(tex.tex, ss.samplerstate, uvX);
    float4 cy = SAMPLE_TEXTURE2D(tex.tex, ss.samplerstate, uvY);
    float4 cz = SAMPLE_TEXTURE2D(tex.tex, ss.samplerstate, uvZ);

    return cx * blend.x + cy * blend.y + cz * blend.z;
}

// Triplanar normal mapping using the "whiteout" blend (Ben Golus), which
// reorients each tangent-space sample onto its projection plane and blends
// in world space. Returns a world-space normal.
float3 Terrain_TriplanarNormal(UnityTexture2D nmap, UnitySamplerState ss,
                               float3 worldPos, float3 worldNormal,
                               float3 blend, float scale, float strength)
{
    float2 uvX = worldPos.zy * scale;
    float2 uvY = worldPos.xz * scale;
    float2 uvZ = worldPos.xy * scale;

    float3 tnormalX = UnpackNormalScale(SAMPLE_TEXTURE2D(nmap.tex, ss.samplerstate, uvX), strength);
    float3 tnormalY = UnpackNormalScale(SAMPLE_TEXTURE2D(nmap.tex, ss.samplerstate, uvY), strength);
    float3 tnormalZ = UnpackNormalScale(SAMPLE_TEXTURE2D(nmap.tex, ss.samplerstate, uvZ), strength);

    // Whiteout blend: keep the geometry normal's sign on the projected axis.
    float3 axisSign = sign(worldNormal);

    tnormalX = float3(tnormalX.xy + worldNormal.zy, abs(tnormalX.z) * worldNormal.x);
    tnormalY = float3(tnormalY.xy + worldNormal.xz, abs(tnormalY.z) * worldNormal.y);
    tnormalZ = float3(tnormalZ.xy + worldNormal.xy, abs(tnormalZ.z) * worldNormal.z);

    float3 worldN = tnormalX.zyx * blend.x +
                    tnormalY.xzy * blend.y +
                    tnormalZ.xyz * blend.z;

    return normalize(worldN);
}

// ---------------------------------------------------------------------
// Custom Function entry point (precision-suffixed for Shader Graph).
//
// Height : the terrain elevation used for the height band. Pass either a
//          normalized 0..1 value, or world Y remapped to 0..1 in the graph
//          (whatever your HeightMin/HeightMax are expressed in).
// Slope  : computed here from the world normal. 0 = flat, 1 = vertical.
// ---------------------------------------------------------------------
void TerrainLayer_float(
    float3 WorldPos,
    float3 WorldNormal,
    float  Height,
    UnityTexture2D Albedo,
    UnityTexture2D NormalMap,
    UnitySamplerState SS,
    float4 Tint,
    float  Tiling,
    float  NormalStrength,
    float  Smoothness,
    float  HeightMin,
    float  HeightMax,
    float  HeightBlend,
    float  SlopeMin,
    float  SlopeMax,
    float  SlopeBlend,
    float  TriplanarSharpness,
    out float3 OutColor,
    out float3 OutNormal,
    out float  OutSmoothness,
    out float  OutWeight)
{
    float3 wn = normalize(WorldNormal);
    float3 blend = Terrain_TriplanarBlend(wn, TriplanarSharpness);

    float4 col = Terrain_TriplanarColor(Albedo, SS, WorldPos, blend, Tiling) * Tint;
    float3 nrm = Terrain_TriplanarNormal(NormalMap, SS, WorldPos, wn, blend, Tiling, NormalStrength);

    // 0 when surface faces straight up, 1 when vertical.
    float slope = 1.0 - saturate(wn.y);

    float hW = Terrain_BandWeight(Height, HeightMin, HeightMax, HeightBlend);
    float sW = Terrain_BandWeight(slope,  SlopeMin,  SlopeMax,  SlopeBlend);

    OutColor      = col.rgb;
    OutNormal     = nrm;
    OutSmoothness = Smoothness;
    OutWeight     = hW * sW;
}

#endif // CODENAME_TERRAIN_LAYERS_INCLUDED
