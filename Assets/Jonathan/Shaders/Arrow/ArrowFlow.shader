// Attack preview arrows: unlit HDR with bright bands that flow toward the arrowhead.
// Arrow.cs feeds _FlowOrigin (the head) and _FlowDir (tail to head) every frame.
Shader "Armadahex/Arrow Flow"
{
    Properties
    {
        [MainColor] [HDR] _BaseColor ("Color", Color) = (0.94, 0.38, 0, 1)
        _Brightness ("Brightness", Float) = 1.3
        _BandSpacing ("Band Spacing", Float) = 14
        _BandSpeed ("Band Speed", Float) = 1.4
        _BandGlow ("Band Glow", Float) = 2.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ArrowFlow"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Brightness;
                float _BandSpacing;
                float _BandSpeed;
                float _BandGlow;
                float4 _FlowOrigin;
                float4 _FlowDir;
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
                // distance along the arrow, 0 at the head, negative down the shaft
                float along = dot(input.positionWS - _FlowOrigin.xyz, _FlowDir.xyz);
                float band = frac(along / _BandSpacing - _Time.y * _BandSpeed);
                float bands = pow(band, 5) * _BandGlow;

                // a little facing shade so the arrow still reads as a solid shape
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float facing = 0.55 + 0.45 * saturate(dot(normalize(input.normalWS), viewDir));

                return half4(_BaseColor.rgb * (_Brightness * facing + bands), 1);
            }
            ENDHLSL
        }
    }
}
