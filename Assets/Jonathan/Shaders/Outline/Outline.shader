Shader "Hidden/Armadahex/Outline"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        TEXTURE2D_X(_OutlineMask);
        float4 _OutlineColor;
        float _OutlineWidth;
        float _OutlineIntensity;
        float4 _OutlineTexelSize;

        half Blur(float2 uv, float2 dir)
        {
            float sigma = max(_OutlineWidth * 0.5, 0.5);
            int radius = (int)_OutlineWidth;
            half sum = 0;
            half weights = 0;

            [loop]
            for (int i = -radius; i <= radius; i++)
            {
                float w = exp(-(i * i) / (2 * sigma * sigma));
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + dir * i * _OutlineTexelSize.xy).r * w;
                weights += w;
            }

            return sum / weights;
        }
        ENDHLSL

        // 0: silhouette of the outlined objects
        Pass
        {
            Name "Mask"

            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment MaskFrag

            struct MaskAttributes { float4 positionOS : POSITION; };
            struct MaskVaryings { float4 positionCS : SV_POSITION; };

            MaskVaryings MaskVert(MaskAttributes input)
            {
                MaskVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 MaskFrag(MaskVaryings input) : SV_Target
            {
                return 1;
            }
            ENDHLSL
        }

        // 1: horizontal blur
        Pass
        {
            Name "BlurH"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                return Blur(input.texcoord, float2(1, 0));
            }
            ENDHLSL
        }

        // 2: vertical blur
        Pass
        {
            Name "BlurV"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                return Blur(input.texcoord, float2(0, 1));
            }
            ENDHLSL
        }

        // 3: draw the blurred edge (outside the silhouette) over the scene
        Pass
        {
            Name "Composite"
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D_X(_OutlineMask, sampler_PointClamp, input.texcoord).r;
                half blur = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord).r;
                half alpha = saturate(blur * _OutlineIntensity) * (1 - mask);
                return half4(_OutlineColor.rgb, _OutlineColor.a * alpha);
            }
            ENDHLSL
        }
    }
}
