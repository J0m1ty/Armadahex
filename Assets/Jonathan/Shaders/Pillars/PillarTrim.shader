Shader "Armadahex/Pillar Trim"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Glow ("Glow", Float) = 2
        _PulseGlow ("Pulse Glow", Float) = 5
        _PulsePeriod ("Pulse Period", Float) = 7
        _PulseWavelength ("Pulse Wavelength", Float) = 260
        _PulseReach ("Pulse Reach", Float) = 450
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _Glow;
            float _PulseGlow;
            float _PulsePeriod;
            float _PulseWavelength;
            float _PulseReach;
            CBUFFER_END

            // xy: where a pulse breathes out from, z: its phase offset (set per board by HexPillarTerrain)
            float4 _PulseSources[3];

            struct Attributes
            {
                float4 positionOS : POSITION;
                // xy: pillar position around the board center
                float2 glowData : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float glow : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                // soft rings breathing out from a few sources, fading the further they travel
                float pulse = 0;
                for (int i = 0; i < 3; i++)
                {
                    float dist = distance(input.glowData.xy, _PulseSources[i].xy);
                    float phase = (_Time.y / _PulsePeriod - dist / _PulseWavelength + _PulseSources[i].z) * TWO_PI;
                    float ring = pow(0.5 + 0.5 * sin(phase), 3);
                    pulse += ring * exp(-dist / _PulseReach);
                }
                output.glow = _Glow + _PulseGlow * saturate(pulse);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 color = _BaseColor.rgb * input.glow;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
