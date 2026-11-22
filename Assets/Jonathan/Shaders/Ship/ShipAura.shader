// Drawn over your ships (ShipAura.cs makes the mesh copies). Every ship shares Resources/Ship Aura.mat,
// so tweaking it in the Inspector during Play mode updates all of them live.
Shader "Armadahex/Ship Aura"
{
    Properties
    {
        [Header(Tint)]
        [HDR] _TintColor ("Tint Color", Color) = (0.05, 0.25, 0.6, 1)
        _TintStrength ("Tint Strength", Range(0, 2)) = 0.12

        [Header(Rim)]
        [HDR] _RimColor ("Rim Color", Color) = (0.2, 0.75, 2.2, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimStrength ("Rim Strength", Range(0, 4)) = 0.3

        [Header(Pulse)]
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.25
        _PulseSpeed ("Pulse Speed", Float) = 0.6

        [Header(Waterline)]
        _WaterLevel ("Water Level", Float) = 0
        _WaterlineGlow ("Waterline Glow", Range(0, 4)) = 0.4
        _WaterlineFalloff ("Waterline Falloff", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Aura"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _TintColor;
                float _TintStrength;
                float4 _RimColor;
                float _RimPower;
                float _RimStrength;
                float _PulseStrength;
                float _PulseSpeed;
                float _WaterLevel;
                float _WaterlineGlow;
                float _WaterlineFalloff;
            CBUFFER_END

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

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float facing = saturate(abs(dot(normalize(input.normalWS), viewDir)));
                float rim = pow(1 - facing, _RimPower);

                // a slow breath, offset a little across the fleet so ships don't pulse in lockstep
                float pulse = 1 - _PulseStrength * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed * 6.2832 + dot(input.positionWS.xz, float2(0.004, 0.003))));

                // where the hull meets the water
                float waterline = exp(-abs(input.positionWS.y - _WaterLevel) * _WaterlineFalloff);

                float3 color = _TintColor.rgb * _TintStrength
                    + _RimColor.rgb * rim * _RimStrength
                    + _RimColor.rgb * waterline * _WaterlineGlow;
                return half4(color * pulse, 0);
            }
            ENDHLSL
        }
    }
}
