Shader "Armadahex/Pillar Trim"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Glow ("Glow", Float) = 2
        _PulseGlow ("Pulse Glow", Float) = 5
        _PulsePeriod ("Pulse Period", Float) = 7
        _PulseWavelength ("Pulse Wavelength", Float) = 260
        _PulseSharpness ("Pulse Sharpness", Float) = 6
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
            float _PulseSharpness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                // x: distance along the contour line this rim is part of
                float2 glowData : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float along : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                output.along = input.glowData.x;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // light running along the contour lines
                float phase = (input.along / _PulseWavelength - _Time.y / _PulsePeriod) * TWO_PI;
                float pulse = pow(0.5 + 0.5 * sin(phase), _PulseSharpness);
                half3 color = _BaseColor.rgb * (_Glow + _PulseGlow * pulse);
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
