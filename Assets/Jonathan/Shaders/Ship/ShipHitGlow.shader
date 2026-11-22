// Drawn over a ship's own meshes (ShotEffects makes the copies). Glows the hull red over hit hexes,
// or everywhere once _GlowAll is set (an enemy ship that has just been revealed).
Shader "Armadahex/Ship Hit Glow"
{
    Properties
    {
        [HDR] _HitColor ("Hit Color", Color) = (2.2, 0.25, 0.1, 1)
        _GlowAll ("Glow Everywhere", Range(0, 1)) = 0
        _HexSize ("Hex Size", Float) = 25
        _Flicker ("Flicker", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "HitGlow"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define SQRT3 1.7320508

            CBUFFER_START(UnityPerMaterial)
                float4 _HitColor;
                float _GlowAll;
                float _HexSize;
                float _Flicker;
            CBUFFER_END

            // set with SetVectorArray: xyz hex centre, w on
            float4 _HitPoints[8];

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            // flat topped hex, negative inside
            float HexDistance(float2 p, float size)
            {
                p = abs(p);
                return max(dot(p, float2(SQRT3 * 0.5, 0.5)), p.y) - size * SQRT3 * 0.5;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float mask = _GlowAll;
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float4 hit = _HitPoints[i];
                    if (hit.w > 0)
                    {
                        float d = HexDistance(input.positionWS.xz - hit.xz, _HexSize);
                        mask = max(mask, 1 - smoothstep(-6, 1, d));
                    }
                }
                if (mask <= 0) discard;

                // brighter on the silhouette, with a slow heat flicker
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float rim = pow(1 - saturate(abs(dot(normalize(input.normalWS), viewDir))), 2);
                float flicker = 1 - _Flicker * (0.5 + 0.5 * sin(_Time.y * 5 + input.positionWS.x * 0.15 + input.positionWS.z * 0.11));

                return half4(_HitColor.rgb * mask * (0.35 + 1.4 * rim) * flicker, 0);
            }
            ENDHLSL
        }
    }
}
