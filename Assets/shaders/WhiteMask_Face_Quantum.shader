Shader "WhiteMask/FaceQuantum"
{
    Properties
    {
        _BaseColor     ("Base Color", Color) = (0.85,0.85,0.9,1)
        _Brightness    ("Brightness", Range(0,1)) = 1
        _Sharpness     ("Sharpness",  Range(0,1)) = 1
        _CollapseLines ("CollapseLines", Range(0,1)) = 0
        _PhaseNoise    ("PhaseNoise",  Range(0,1)) = 0
        _OpacityShift  ("OpacityShift",Range(0,1)) = 0
        _MainTex       ("Face Albedo (optional)", 2D) = "white" {}
        _NoiseTex      ("Noise (for re-phase)", 2D) = "gray" {}
    }
    SubShader
    {
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _BaseColor;
            float _Brightness;
            float _Sharpness;
            float _CollapseLines;
            float _PhaseNoise;
            float _OpacityShift;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float3 normal : NORMAL;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float3 n   : TEXCOORD1;
                float3 wpos: TEXCOORD2;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos  = UnityObjectToClipPos(v.vertex);
                o.uv   = v.uv;
                o.n    = UnityObjectToWorldNormal(v.normal);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // Pseudo "sharpness": use smoothing/contrast to suggest "being seen clearly" (no real blur)
            float3 ApplySharpness(float3 c, float sharp){
                // sharp=1 stronger contrast; sharp=0 slightly greyed
                float3 mid = float3(0.5,0.5,0.5);
                return lerp(lerp(c, mid, 0.35), c*1.12, sharp);
            }

            // Scan lines: the higher CollapseLines, the more visible
            float CollapseStripe(float2 uv, float strength){
                float t = _Time.y * 2.0;
                float lines = sin((uv.y + t) * 120.0);     // thin horizontal stripes
                lines = smoothstep(0.6, 0.95, lines);      // keep only the bright stripes
                return lerp(0.0, lines, strength);
            }

            // Phase distortion: UV warp controlled by PhaseNoise (the "self-rewriting" feel of re-phasing)
            float2 PhaseWarp(float2 uv, float strength){
                if (strength <= 0.001) return uv;
                float2 n1 = tex2D(_NoiseTex, uv*1.3 + _Time.y*0.07).rg;
                float2 n2 = tex2D(_NoiseTex, uv*2.1 - _Time.y*0.05).rg;
                float2 flow = (n1 - 0.5 + n2 - 0.5) * 0.12;
                return uv + flow * strength;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample after phase distortion
                float2 uvW = PhaseWarp(i.uv, _PhaseNoise);

                float3 baseCol = _BaseColor.rgb;
                float3 albedo  = tex2D(_MainTex, uvW).rgb;
                float3 face    = lerp(baseCol, albedo, 0.85);

                // "Sharpness / legibility"
                face = ApplySharpness(face, _Sharpness);

                // Brightness (brighter when measured: being seen means being "exposed")
                face *= lerp(0.5, 1.6, _Brightness);

                // Scan-line overlay (lines add brightness)
                float stripe = CollapseStripe(uvW, _CollapseLines);
                face = saturate(face + stripe * 0.35);

                // Transparency (withdrawal / abstraction)
                float alpha = saturate(1.0 - _OpacityShift); // higher shift = fainter

                return float4(face, alpha);
            }
            ENDCG
        }
    }
}