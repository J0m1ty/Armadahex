// Hover / lock-in column around a hex (HexBorder walls).
// Additive, so it never fights transparent sorting with the water or the cloud fog.
Shader "Armadahex/Hex Highlight"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.15, 0.1, 1)
        _Intensity ("Intensity", Float) = 2.5
        _Scale ("Lock Amount (set by HexBorder)", Float) = 0.04
        _WaterLevel ("Water Level", Float) = 0
        _WaterLine ("Water Line Glow", Float) = 2.2
        _BandCount ("Band Count", Float) = 3
        _BandSpeed ("Band Speed", Float) = 0.6
        _BandGlow ("Band Glow", Float) = 0.7
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Highlight"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _Scale;
                float _WaterLevel;
                float _WaterLine;
                float _BandCount;
                float _BandSpeed;
                float _BandGlow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float height : TEXCOORD1; // 0 at the base, 1 at the top
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.height = input.uv.x;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float v = saturate(input.height);

                // HexBorder maps lock-in height to _Scale 0.2 and hover to 0.04
                float locked = saturate((_Scale - 0.04) / 0.16);

                // hover is a short ring, lock-in a tall beam that should still show above the clouds
                float fade = pow(1 - v, lerp(2.5, 0.8, locked));
                float waterLine = exp(-abs(input.positionWS.y - _WaterLevel) * 0.5) * _WaterLine;
                float wave = frac(v * _BandCount - _Time.y * _BandSpeed * (1 + locked));
                float bands = pow(1 - wave, 6) * _BandGlow * (1 - v);

                float glow = (fade * 0.7 + waterLine + bands) * fade;
                return half4(_Color.rgb * glow * _Intensity * lerp(1, 1.4, locked), 0);
            }
            ENDHLSL
        }
    }
}
