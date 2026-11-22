// Fog of war as a low raymarched cloud deck over one board.
// CloudFog.cs feeds it a per hex state map (rgb glow, a cover) and a shared 3d noise.
Shader "Armadahex/Cloud Fog"
{
    Properties
    {
        [Header(Shape)]
        _Density ("Density", Float) = 0.35
        _ShapeScale ("Shape Scale", Float) = 0.004
        _DetailScale ("Detail Scale", Float) = 0.018
        _DetailErode ("Detail Erode", Range(0, 1)) = 0.4
        _Coverage ("Coverage Bias", Range(0, 1)) = 0.85
        _TopNoise ("Top Height Noise", Range(0, 1)) = 0.6
        _Wind ("Wind (xyz shape, w detail speed)", Vector) = (5, 0.4, 2.5, 9)

        [Header(Hex Edges)]
        _Warp ("Edge Warp", Float) = 14
        _ColumnSoftness ("Column Softness", Float) = 14

        [Header(Lighting)]
        _LitColor ("Lit Color", Color) = (1, 0.97, 0.93, 1)
        _ShadowColor ("Shadow Color", Color) = (0.5, 0.55, 0.66, 1)
        _AmbientColor ("Ambient Color", Color) = (0.75, 0.8, 0.9, 1)
        _SunStrength ("Sun Strength", Float) = 0.8
        _Absorption ("Light Absorption", Float) = 2.5
        _SilverLining ("Silver Lining", Range(0, 1)) = 0.55
        _GlowStrength ("Glow Strength", Float) = 1.6

        [Header(March)]
        _Steps ("Steps", Range(8, 64)) = 40
        _MaxDistance ("Max March Distance", Float) = 700
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+50" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Clouds"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE3D(_CloudNoise); SAMPLER(sampler_CloudNoise);
            TEXTURE2D(_CloudMap); SAMPLER(sampler_CloudMap); // point, clamp, set on the texture

            CBUFFER_START(UnityPerMaterial)
                float _Density;
                float _ShapeScale;
                float _DetailScale;
                float _DetailErode;
                float _Coverage;
                float _TopNoise;
                float4 _Wind;
                float _Warp;
                float _ColumnSoftness;
                float4 _LitColor;
                float4 _ShadowColor;
                float4 _AmbientColor;
                float _SunStrength;
                float _Absorption;
                float _SilverLining;
                float _GlowStrength;
                float _Steps;
                float _MaxDistance;
                // set from CloudFog.cs
                float4 _BoxMin;
                float4 _BoxMax;
                float4 _GridOrigin;
                float4 _MapParams; // size, offset, hex size
                float4 _Column;    // x, z, top y, on
            CBUFFER_END

            #define SQRT3 1.7320508

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            float4 MapAt(float2 axial)
            {
                float2 texel = axial + _MapParams.y;
                if (any(texel < 0) || any(texel >= _MapParams.x)) return 0;
                return SAMPLE_TEXTURE2D_LOD(_CloudMap, sampler_CloudMap, (texel + 0.5) / _MapParams.x, 0);
            }

            // signed distance to a flat topped hex of circumradius size, negative inside
            float HexDistance(float2 p, float size)
            {
                p = abs(p);
                float inner = size * SQRT3 * 0.5;
                return max(dot(p, float2(SQRT3 * 0.5, 0.5)), p.y) - inner;
            }

            float2 AxialCenter(float2 axial)
            {
                float size = _MapParams.z;
                return float2(size * 1.5 * axial.x, size * SQRT3 * (axial.y + axial.x * 0.5));
            }

            // smooth bump that reaches zero at the distance to an edge midpoint of the next triangle
            float Kernel(float2 p, float2 axial)
            {
                float reach = _MapParams.z * 1.5; // 0.866 of the centre spacing
                float2 d = (p - AxialCenter(axial)) / reach;
                float k = saturate(1 - dot(d, d));
                return k * k;
            }

            // state at p, blended between the three nearest hex centres with round kernels,
            // so a single open hex reads as a soft circle rather than a hexagon.
            // axial (1,0) and (0,1) are neighbours, so each axial cell splits into two triangles.
            // same axial maths as CloudFog.AxialFromLocal
            float4 HexState(float2 p)
            {
                float size = _MapParams.z;
                float2 axial = float2(2.0 / 3.0 * p.x, -1.0 / 3.0 * p.x + SQRT3 / 3.0 * p.y) / size;
                float2 cell = floor(axial);
                float2 f = axial - cell;

                float2 a = f.x + f.y < 1 ? cell : cell + 1;
                float2 b = cell + float2(1, 0);
                float2 c = cell + float2(0, 1);

                float wa = Kernel(p, a);
                float wb = Kernel(p, b);
                float wc = Kernel(p, c);
                return (MapAt(a) * wa + MapAt(b) * wb + MapAt(c) * wc) / max(wa + wb + wc, 1e-4);
            }

            float Dither(float2 pixel)
            {
                return frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
            }

            float Remap(float v, float lo, float hi)
            {
                return saturate((v - lo) / max(hi - lo, 1e-4));
            }

            // density at a grid local point, glow returned through the out param
            float Density(float3 p, float time, out float3 glow, bool detail)
            {
                float3 shapeP = p * _ShapeScale + _Wind.xyz * _ShapeScale * time;
                float4 shape = SAMPLE_TEXTURE3D_LOD(_CloudNoise, sampler_CloudNoise, shapeP, 0);

                float2 warp = (float2(shape.b, SAMPLE_TEXTURE3D_LOD(_CloudNoise, sampler_CloudNoise, shapeP.zxy + 0.37, 0).b) - 0.5) * 2 * _Warp;
                float4 state = HexState(p.xz + warp);
                glow = state.rgb;

                float cover = state.a;
                if (cover <= 0.001) return 0;

                // a heightfield of soft mounds: tops follow the noise and sink as cover drops,
                // so cleared hexes leave sloped valleys rather than walls
                float h = (p.y - _BoxMin.y) / (_BoxMax.y - _BoxMin.y);
                float heap = shape.r * 0.6 + shape.b * 0.4;
                float top = lerp(1 - _TopNoise, 1, heap) * smoothstep(0, 1, cover);
                float profile = saturate(h / 0.08) * saturate((top - h) / 0.3);

                float base = Remap(shape.r, 1 - _Coverage * cover, 1) * profile;
                if (base <= 0) return 0;

                if (detail)
                {
                    float3 detailP = p * _DetailScale + float3(0, -1, 0.3) * _Wind.w * _DetailScale * time;
                    float d = SAMPLE_TEXTURE3D_LOD(_CloudNoise, sampler_CloudNoise, detailP, 0).g;
                    base = Remap(base, d * _DetailErode * (1 - base * 0.6), 1);
                }

                return base;
            }

            bool BoxHit(float3 ro, float3 rd, out float t0, out float t1)
            {
                float3 inv = 1 / rd;
                float3 a = (_BoxMin.xyz - ro) * inv;
                float3 b = (_BoxMax.xyz - ro) * inv;
                float3 lo = min(a, b);
                float3 hi = max(a, b);
                t0 = max(max(lo.x, lo.y), max(lo.z, 0));
                t1 = min(min(hi.x, hi.y), hi.z);
                return t1 > t0;
            }

            float HenyeyGreenstein(float cosAngle, float g)
            {
                float g2 = g * g;
                return (1 - g2) / (4 * PI * pow(abs(1 + g2 - 2 * g * cosAngle), 1.5));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 camWS = GetCameraPositionWS();
                float3 rdWS = normalize(input.positionWS - camWS);
                float3 ro = camWS - _GridOrigin.xyz;
                float3 rd = rdWS;

                float t0, t1;
                if (!BoxHit(ro, rd, t0, t1)) discard;

                // stop at anything opaque, like pillars and ships
                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
                float eye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float3 forward = -UNITY_MATRIX_V[2].xyz;
                t1 = min(t1, eye / max(dot(rdWS, forward), 1e-3));
                t1 = min(t1, t0 + _MaxDistance);
                if (t1 <= t0) discard;

                Light sun = GetMainLight();
                float3 sunDir = sun.direction;
                float cosAngle = dot(rd, sunDir);
                float phase = HenyeyGreenstein(cosAngle, 0.7) * 4 * PI * _SilverLining;
                // the game sun is a deep orange, keep the clouds mostly white
                float3 sunColor = lerp(1, sun.color, 0.35) * _SunStrength;

                int steps = (int)_Steps;
                float marchLength = t1 - t0;
                // jitter the start so banding turns into fine noise
                float jitter = Dither(input.positionCS.xy);
                float time = _Time.y;

                float transmittance = 1;
                float3 light = 0;

                [loop]
                for (int i = 0; i < steps; i++)
                {
                    // samples bunch up near the camera, where detail shows, and spread out far away
                    float u0 = (i + jitter) / steps;
                    float u1 = (i + 1 + jitter) / steps;
                    float t = t0 + marchLength * u0 * u0;
                    float dt = marchLength * (u1 * u1 - u0 * u0);
                    float3 p = ro + rd * t;

                    float3 glow;
                    float density = Density(p, time, glow, true);
                    // part the clouds softly around the hover column, its fading top dissolves into them
                    if (_Column.w > 0)
                    {
                        float columnDistance = max(HexDistance(p.xz - _Column.xy, _MapParams.z), p.y - _Column.z);
                        density *= smoothstep(0, _ColumnSoftness, columnDistance);
                    }
                    if (density <= 0.001) continue;

                    // two short taps toward the sun for self shadowing
                    float3 unused;
                    float toSun = Density(p + sunDir * 6, time, unused, false) + Density(p + sunDir * 16, time, unused, false);
                    float sunLight = exp(-toSun * _Absorption);

                    float h = saturate((p.y - _BoxMin.y) / (_BoxMax.y - _BoxMin.y));
                    // dark undersides, bright tops, a touch of sun on the lit faces
                    float3 albedo = lerp(_ShadowColor.rgb, _LitColor.rgb, saturate(sunLight * 0.7 + h * 0.5));
                    float3 ambient = _AmbientColor.rgb * lerp(0.7, 1.15, h);
                    float3 direct = sunColor * sunLight * (0.55 + phase);
                    // powder: thin wisps darken, so the billows read as volume
                    float powder = 1 - exp(-density * _Density * 30);
                    float3 color = albedo * (ambient + direct) * lerp(0.75, 1, powder);
                    // glow is strongest low in the deck, like light caught underneath
                    color += glow * _GlowStrength * lerp(1.4, 0.5, h);

                    float extinction = density * _Density;
                    float stepT = exp(-extinction * dt);
                    light += transmittance * (1 - stepT) * color;
                    transmittance *= stepT;

                    if (transmittance < 0.02) break;
                }

                float alpha = 1 - transmittance;
                return half4(light, alpha);
            }
            ENDHLSL
        }
    }
}
