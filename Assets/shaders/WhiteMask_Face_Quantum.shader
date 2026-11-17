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

            // 伪“锐度”：用平滑/对比来模拟“被看清”的感觉（不做真正模糊）
            float3 ApplySharpness(float3 c, float sharp){
                // sharp=1 对比更强；sharp=0 轻微灰化
                float3 mid = float3(0.5,0.5,0.5);
                return lerp(lerp(c, mid, 0.35), c*1.12, sharp);
            }

            // 扫描线：CollapseLines 越高，线越明显
            float CollapseStripe(float2 uv, float strength){
                float t = _Time.y * 2.0;
                float lines = sin((uv.y + t) * 120.0);     // 纵向细条
                lines = smoothstep(0.6, 0.95, lines);      // 只保留亮条
                return lerp(0.0, lines, strength);
            }

            // 相位扰动：PhaseNoise 控制的 UV 扭曲（re-phase 的“自我重写”感）
            float2 PhaseWarp(float2 uv, float strength){
                if (strength <= 0.001) return uv;
                float2 n1 = tex2D(_NoiseTex, uv*1.3 + _Time.y*0.07).rg;
                float2 n2 = tex2D(_NoiseTex, uv*2.1 - _Time.y*0.05).rg;
                float2 flow = (n1 - 0.5 + n2 - 0.5) * 0.12;
                return uv + flow * strength;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 相位扰动后的取样
                float2 uvW = PhaseWarp(i.uv, _PhaseNoise);

                float3 baseCol = _BaseColor.rgb;
                float3 albedo  = tex2D(_MainTex, uvW).rgb;
                float3 face    = lerp(baseCol, albedo, 0.85);

                // “锐度/可辨识度”
                face = ApplySharpness(face, _Sharpness);

                // 亮度（被测量更亮、被看见更“暴露”）
                face *= lerp(0.5, 1.6, _Brightness);

                // 扫描线覆盖（线本身是亮度叠加）
                float stripe = CollapseStripe(uvW, _CollapseLines);
                face = saturate(face + stripe * 0.35);

                // 透明度（抽身/抽象化）
                float alpha = saturate(1.0 - _OpacityShift); // shift 越高越淡

                return float4(face, alpha);
            }
            ENDCG
        }
    }
}