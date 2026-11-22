// shoreline foam from the baked pillar distance map (see ShoreFoam.cs)
#ifndef SHORE_FOAM_INCLUDED
#define SHORE_FOAM_INCLUDED

TEXTURE2D(_ShoreFoamMap);
SAMPLER(sampler_ShoreFoamMap);
// xy = world min corner, zw = 1 / world size
float4 _ShoreFoamRect;
// x = intensity, y = band count, z = band speed, w = solid edge width (0-1)
float4 _ShoreFoamParams;

float ShoreFoamHash(float2 p) {
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float ShoreFoamNoise(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3 - 2 * f);
    float a = ShoreFoamHash(i);
    float b = ShoreFoamHash(i + float2(1, 0));
    float c = ShoreFoamHash(i + float2(0, 1));
    float d = ShoreFoamHash(i + float2(1, 1));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

void ShoreFoam_float(float3 WorldPos, out float3 Out) {
    Out = 0;
    if (_ShoreFoamRect.z == 0) return;

    float2 uv = (WorldPos.xz - _ShoreFoamRect.xy) * _ShoreFoamRect.zw;
    if (any(uv < 0) || any(uv > 1)) return;

    float4 map = SAMPLE_TEXTURE2D(_ShoreFoamMap, sampler_ShoreFoamMap, uv);
    // 1 right at a pillar, 0 at the outer edge of the foam
    float d = map.a;

    // slow drifting noise so the foam isn't the same all the way around
    float n = ShoreFoamNoise(WorldPos.xz * 0.06 + _Time.y * 0.03);

    // a solid line hugging the pillar, then bands that roll outward and fade
    float edge = smoothstep(1 - _ShoreFoamParams.w, 1 - _ShoreFoamParams.w * 0.5, d - n * 0.15);
    float bands = sin((1 - d) * _ShoreFoamParams.y * 6.2832 + _Time.y * _ShoreFoamParams.z + n * 4);
    bands = smoothstep(0.55, 0.95, bands) * d * smoothstep(0.25, 0.75, n);

    Out = map.rgb * saturate(edge + bands) * _ShoreFoamParams.x;
}

void ShoreFoam_half(half3 WorldPos, out half3 Out) {
    float3 result;
    ShoreFoam_float(WorldPos, result);
    Out = result;
}

#endif
