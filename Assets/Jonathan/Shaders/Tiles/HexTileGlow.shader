// Your own board's hex tiles: a dark under-water floor with a faint glow along each tile's top edges,
// a little on the top face and a little down the sides, like the pillar trim.
// HexGrid.UseGlowTiles sets _TopY per tile (tiles sit at slightly different heights).
Shader "Armadahex/Hex Tile Glow"
{
    Properties
    {
        _BaseColor ("Body Tint", Color) = (0.5, 0.6, 0.8, 1)
        _Darkness ("Body Brightness", Range(0, 1)) = 0.35
        [HDR] _EdgeColor ("Edge Color", Color) = (0.1, 0.35, 1, 1)
        _EdgeTop ("Edge Width On Top", Float) = 1.6
        _EdgeSide ("Edge Depth Down The Sides", Float) = 2.5
        _HexSize ("Hex Size", Float) = 25
        _TopY ("Tile Top (world y)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "HexTile"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define SQRT3 1.7320508

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Darkness;
                float4 _EdgeColor;
                float _EdgeTop;
                float _EdgeSide;
                float _HexSize;
                float _TopY;
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
                float3 positionOS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.positionOS = input.positionOS.xyz;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            // flat topped hex of circumradius size, negative inside
            float HexDistance(float2 p, float size)
            {
                p = abs(p);
                return max(dot(p, float2(SQRT3 * 0.5, 0.5)), p.y) - size * SQRT3 * 0.5;
            }

            half4 frag(Varyings input, bool frontFace : SV_IsFrontFace) : SV_Target
            {
                float3 normal = normalize(input.normalWS) * (frontFace ? 1 : -1);
                bool top = abs(normal.y) > 0.5;

                float edge;
                if (top)
                {
                    // the tile's own object space is centred on the hex
                    edge = 1 - smoothstep(0, _EdgeTop, -HexDistance(input.positionOS.xz, _HexSize));
                }
                else
                {
                    edge = 1 - smoothstep(0, _EdgeSide, _TopY - input.positionWS.y);
                }

                Light sun = GetMainLight();
                float diffuse = saturate(dot(normal, sun.direction)) * 0.6 + 0.4;
                float3 body = _BaseColor.rgb * _Darkness * diffuse * lerp(1, sun.color, 0.3);

                return half4(body + _EdgeColor.rgb * edge, 1);
            }
            ENDHLSL
        }
    }
}
