Shader "Hidden/Armadahex/Backdrop"
{
    // the shared menu background, blitted into a texture by the Backdrop element
    Properties
    {
        _Base ("Base", Color) = (0.012, 0.02, 0.04, 1)
        _Light ("Light", Color) = (0.06, 0.2, 0.55, 1)
        _Edge ("Edge Strength", Float) = 0.275
        _Effect ("Effect", Float) = 0
        _Aspect ("Aspect", Float) = 1.777
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Base;
            float4 _Light;
            float _Edge;
            float _Effect;
            float _Aspect;

            float hash(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // distance to the nearest hex cell edge, 0 on the line
            float hexEdge(float2 p) {
                const float2 s = float2(1, 1.7320508);
                float4 c = floor(float4(p, p - float2(0.5, 1)) / s.xyxy) + 0.5;
                float4 h = float4(p - c.xy * s, p - (c.zw + 0.5) * s);
                float2 q = dot(h.xy, h.xy) < dot(h.zw, h.zw) ? h.xy : h.zw;
                q = abs(q);
                return 0.5 - max(dot(q, normalize(s)), q.x);
            }

            float caustic(float2 p, float t) {
                float v = 0;
                float2 q = p;
                for (int i = 0; i < 3; i++) {
                    q += float2(sin(q.y * 1.7 + t * 0.6), cos(q.x * 1.3 - t * 0.5)) * 0.45;
                    v += abs(sin(q.x * 2.1 + q.y * 1.4));
                }
                return pow(saturate(1 - v / 3), 6);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;
                float2 p = (uv - 0.5) * float2(_Aspect, 1);

                // light from the edges: a soft frame plus two slow drifting pools in opposite corners
                // each axis lights on its own and they blend, so the corners stay smooth instead of creasing on the diagonal
                float fx0 = exp(-min(uv.x, 1 - uv.x) * _Aspect * 7);
                float fy0 = exp(-min(uv.y, 1 - uv.y) * 7);
                float frame = 1 - (1 - fx0) * (1 - fy0);
                float2 a = float2(-0.62 * _Aspect, -0.55) + float2(sin(t * 0.07), cos(t * 0.05)) * 0.08;
                float2 b = float2(0.62 * _Aspect, 0.55) + float2(cos(t * 0.06), sin(t * 0.08)) * 0.08;
                float pools = exp(-length(p - a) * 2.2) + exp(-length(p - b) * 2.6) * 0.8;
                float glow = (frame * 0.6 + pools) * _Edge;

                // the effect: faint, brighter towards the edges so the center stays clean for text
                float fx = 0;
                float reach = 0.35 + glow;
                if (_Effect < 0.5) {
                    // hex lattice with a slow diagonal wave running through it
                    float2 g = p * 16;
                    float stroke = 1 - smoothstep(0, 0.04, hexEdge(g));
                    float wave = pow(0.5 + 0.5 * sin(dot(p, float2(0.8, 0.6)) * 3 - t * 0.5), 8);
                    fx = stroke * (0.05 + wave * 0.25) * reach;
                }
                else if (_Effect < 1.5) {
                    // light through water
                    fx = caustic(p * 3.2, t * 0.6) * 0.5 * reach;
                }
                else {
                    // sonar rings from below the screen
                    float r = length(p - float2(0, -0.9));
                    float ring = frac(r * 2.2 - t * 0.12);
                    fx = pow(ring, 30) * 0.6 * reach + pow(ring, 4) * 0.06 * reach;
                }

                float3 col = _Base.rgb + _Light.rgb * (glow + fx);

                // grain, so the dark gradients don't band
                col += (hash(uv * 1000 + frac(t)) - 0.5) / 255;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
