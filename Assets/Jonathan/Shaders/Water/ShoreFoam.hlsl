// shoreline foam from the baked pillar distance map (see ShoreFoam.cs)
#ifndef SHORE_FOAM_INCLUDED
#define SHORE_FOAM_INCLUDED

TEXTURE2D(_ShoreFoamMap);
SAMPLER(sampler_ShoreFoamMap);
// xy = world min corner, zw = 1 / world size
float4 _ShoreFoamRect;
// x = intensity, y = band count, z = band speed, w = solid edge width (0-1)
float4 _ShoreFoamParams;

void ShoreFoam_float(float3 WorldPos, out float3 Out) {
    Out = 0;
    if (_ShoreFoamRect.z == 0) return;

    float2 uv = (WorldPos.xz - _ShoreFoamRect.xy) * _ShoreFoamRect.zw;
    if (any(uv < 0) || any(uv > 1)) return;

    float4 map = SAMPLE_TEXTURE2D(_ShoreFoamMap, sampler_ShoreFoamMap, uv);
    // 1 right at a pillar, 0 at the outer edge of the foam
    float d = map.a;

    // a solid line hugging the pillar, then bands that roll outward and fade
    float edge = smoothstep(1 - _ShoreFoamParams.w, 1 - _ShoreFoamParams.w * 0.5, d);
    float bands = sin((1 - d) * _ShoreFoamParams.y * 6.2832 + _Time.y * _ShoreFoamParams.z);
    bands = smoothstep(0.4, 0.85, bands) * d;

    Out = map.rgb * saturate(edge + bands) * _ShoreFoamParams.x;
}

void ShoreFoam_half(half3 WorldPos, out half3 Out) {
    float3 result;
    ShoreFoam_float(WorldPos, result);
    Out = result;
}

#endif
