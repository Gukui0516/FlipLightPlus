Shader "Custom/FlashlightUltra"
{
    Properties
    {
        _Color ("Flashlight Color", Color) = (1, 0.9, 0.6, 1)
        _CenterBrightness ("Center Brightness", Range(1, 8)) = 4.0
        _GradientPower ("Gradient Power", Range(0.5, 5)) = 2.5
        _Glow ("Core Glow", Range(0, 5)) = 2.0
        
        [Header(Animation)]
        _FlickerSpeed ("Flicker Speed", Range(0, 20)) = 8.0
        _FlickerAmount ("Flicker Amount", Range(0, 0.3)) = 0.08
        
        [Header(Noise)]
        _NoiseScale ("Noise Scale", Range(1, 30)) = 10.0
        _NoiseStrength ("Noise Strength", Range(0, 0.5)) = 0.2
        _NoiseSpeed ("Noise Speed", Range(0, 2)) = 0.5
        
        [Header(Edge)]
        _EdgeFade ("Edge Fade", Range(0.1, 1)) = 0.5
        _InnerRadius ("Inner Bright Radius", Range(0, 0.5)) = 0.15
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 localPos : TEXCOORD2;  // 로컬 좌표 추가
            };

            float4 _Color;
            float _CenterBrightness;
            float _GradientPower;
            float _Glow;
            float _FlickerSpeed;
            float _FlickerAmount;
            float _NoiseScale;
            float _NoiseStrength;
            float _NoiseSpeed;
            float _EdgeFade;
            float _InnerRadius;
            float _ViewRadius;  // 손전등 실제 반경

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.localPos = v.vertex.xyz;  // 로컬 좌표 저장
                return o;
            }

            // 간단하고 빠른 노이즈 함수
            float simpleNoise(float2 uv)
            {
                float2 p = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                
                float n = p.x + p.y * 57.0;
                float a = frac(sin(n) * 43758.5453);
                float b = frac(sin(n + 1.0) * 43758.5453);
                float c = frac(sin(n + 57.0) * 43758.5453);
                float d = frac(sin(n + 58.0) * 43758.5453);
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 다층 노이즈
            float multiNoise(float2 uv, float time)
            {
                float n = 0.0;
                n += simpleNoise(uv) * 0.5;
                n += simpleNoise(uv * 2.3 - time * 0.3) * 0.3;
                n += simpleNoise(uv * 5.7 + time * 0.2) * 0.2;
                return n;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ✨ 수정: 로컬 좌표 기준으로 거리 계산 (원점이 손전등 시작점)
                float2 localPos2D = i.localPos.xy;
                float dist = length(localPos2D);
                
                // 최대 거리를 기준으로 정규화 (메시 크기에 상관없이 일정한 효과)
                // EasyVisionCone의 viewRadius를 기준으로 0~1 범위로 변환
                float normalizedDist = dist / 10.0;  // 기본 반경 10 기준
                
                // === 1. 메인 그라디언트 (물리적 감쇠) ===
                float attenuation = 1.0 / (1.0 + normalizedDist * normalizedDist * 3.0);
                float gradient = pow(attenuation, _GradientPower);
                
                // === 2. 내부 밝은 영역 ===
                float innerBright = saturate(1.0 - normalizedDist / _InnerRadius);
                innerBright = pow(innerBright, 3.0);
                
                // === 3. 중앙 글로우 (전구 효과) ===
                float coreGlow = pow(saturate(1.0 - normalizedDist * 4.0), 6.0) * _Glow;
                
                // === 4. 가장자리 부드러운 페이드 ===
                float edgeDist = saturate((1.0 - normalizedDist) / _EdgeFade);
                float edgeFade = pow(edgeDist, 2.5);
                
                // === 5. 시간 기반 애니메이션 ===
                float time = _Time.y;
                
                // 깜빡임 (여러 주파수 합성)
                float flicker = 1.0;
                flicker += sin(time * _FlickerSpeed) * _FlickerAmount;
                flicker += sin(time * _FlickerSpeed * 2.7) * _FlickerAmount * 0.5;
                flicker += sin(time * _FlickerSpeed * 0.7) * _FlickerAmount * 0.3;
                flicker = saturate(flicker);
                
                // === 6. 노이즈 (빛의 자연스러운 변화) ===
                float2 noiseUV = i.worldPos.xy * _NoiseScale;
                float noise = multiNoise(noiseUV, time * _NoiseSpeed);
                noise = (noise - 0.5) * _NoiseStrength;
                
                // === 7. 최종 밝기 계산 ===
                float brightness = 0.0;
                brightness += gradient * _CenterBrightness;  // 기본 그라디언트
                brightness += innerBright * 2.0;             // 내부 밝은 영역
                brightness += coreGlow;                      // 중앙 글로우
                brightness += noise;                         // 노이즈 추가
                brightness *= flicker;                       // 깜빡임 적용
                brightness = max(0.0, brightness);
                
                // === 8. 색상 적용 ===
                float4 finalColor = _Color;
                finalColor.rgb *= brightness;
                finalColor.a = gradient * edgeFade;
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}