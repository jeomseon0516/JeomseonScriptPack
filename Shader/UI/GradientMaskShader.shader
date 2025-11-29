Shader "UI/RoundedEdgeWithFade_Masked"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        // 기본 색상들
        _FillColor    ("Fill Color",    Color) = (1,1,1,1)
        _EdgeColor    ("Edge Color",    Color) = (0.8,0.8,0.8,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)

        // 코너 (라운드 사각형)
        _CornerRadius    ("Corner Radius",    Range(0, 0.5)) = 0.1
        _CornerSoftness  ("Corner Softness",  Range(0, 0.1)) = 0.01

        // 엣지 / 윤곽선
        _OutlineThickness ("Outline Thickness", Range(0, 0.3)) = 0.02
        _EdgeThickness    ("Edge Thickness",    Range(0, 0.5)) = 0.1
        _EdgeGradient     ("Edge Gradient",     Range(0, 1))    = 1.0

        // 내부 컨텐츠 영역 패딩 (UV 기준: 0~1)
        _Padding ("Padding L,R,T,B", Vector) = (0, 0, 0, 0)

        // 페이드 모드 (전체 페이드)
        [KeywordEnum(None, Radial, Linear)]
        _FadeMode ("Fade Mode", Float) = 0

        _FadeCenter ("Fade Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _FadeRadius ("Fade Radius", Range(0, 1)) = 0.5
        _FadePower  ("Fade Power",  Range(0, 10)) = 1.0

        // ===== Mask(스텐실)용 숨김 프로퍼티들 =====
        [HideInInspector]_StencilComp      ("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil          ("Stencil ID",         Float) = 0
        [HideInInspector]_StencilOp        ("Stencil Operation",  Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask  ("Stencil Read Mask",  Float) = 255
        [HideInInspector]_ColorMask        ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            // ===== Mask 컴포넌트용 스텐실 블록 =====
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }

            ColorMask [_ColorMask]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 페이드 모드 키워드
            #pragma multi_compile _FADEMODE_NONE _FADEMODE_RADIAL _FADEMODE_LINEAR

            // RectMask2D / Mask 공통 UI 키워드
            #pragma multi_compile __ UNITY_UI_CLIP_RECT UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;

                float4 worldPosition : TEXCOORD1;  // RectMask2D / Mask
                half4  mask          : TEXCOORD2;  // RectMask2D Softness 계산용
            };

            sampler2D _MainTex;
            float4   _MainTex_ST;

            fixed4 _FillColor;
            fixed4 _EdgeColor;
            fixed4 _OutlineColor;

            float _CornerRadius;
            float _CornerSoftness;

            float _OutlineThickness;
            float _EdgeThickness;
            float _EdgeGradient;

            float4 _Padding; // (L, R, T, B)

            float4 _FadeCenter;
            float  _FadeRadius;
            float  _FadePower;

            // RectMask2D에서 셰이더로 넘겨주는 값들
            float4 _ClipRect;
            float  _UIMaskSoftnessX;
            float  _UIMaskSoftnessY;

            v2f vert (appdata v)
            {
                v2f o;

                // UI/Default와 동일한 패턴
                float4 vPos = UnityObjectToClipPos(v.vertex);
                o.vertex = vPos;
                o.worldPosition = v.vertex;                // Canvas 로컬 좌표

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;

                // ===== RectMask2D Softness용 mask.xy / mask.zw 계산 =====
                float2 pixelSize = vPos.w;
                // 프로젝션 행렬 + 스크린 사이즈 기반 픽셀 크기
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                o.mask.xy = v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw;
                o.mask.zw = 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy));

                return o;
            }

            // 라운드 사각형 SDF (innerUV: 0~1)
            float RoundedRectSDF(float2 uv, float cornerRadius)
            {
                float2 p = uv - 0.5;
                float2 halfSize = float2(0.5, 0.5);
                float r = cornerRadius;

                float2 q = abs(p) - (halfSize - r);
                float outsideDist = length(max(q, 0.0)) - r;
                return outsideDist;
            }

            // 전체 페이드 (Radial / Linear / None)
            float ComputeFadeMask(float2 uv)
            {
                #if defined(_FADEMODE_RADIAL)
                    float2 c = _FadeCenter.xy;
                    float d = distance(uv, c);
                    float t = 1.0 - saturate(d / max(_FadeRadius, 1e-4));
                    return pow(saturate(t), _FadePower);
                #elif defined(_FADEMODE_LINEAR)
                    float t = 1.0 - uv.y;
                    return pow(saturate(t), _FadePower);
                #else
                    return 1.0;
                #endif
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 1) 패딩 적용된 inner UV
                float left   = _Padding.x;
                float right  = _Padding.y;
                float top    = _Padding.z;
                float bottom = _Padding.w;

                float innerW = max(1.0 - left - right, 1e-4);
                float innerH = max(1.0 - top  - bottom, 1e-4);

                float2 innerUV;
                innerUV.x = (uv.x - left)   / innerW;
                innerUV.y = (uv.y - bottom) / innerH;

                // 2) 라운드 사각형 SDF
                float d = RoundedRectSDF(innerUV, _CornerRadius);

                float soft = max(_CornerSoftness, 1e-4);
                float shapeMask = 1.0 - smoothstep(0.0, soft, d);

                float insideDist = max(-d, 0.0);

                // 3) 윤곽선
                float outlineOuter = _OutlineThickness;
                float outlineInner = 0.0;
                float outlineMask = 1.0 - smoothstep(outlineInner, outlineOuter, insideDist);
                outlineMask *= shapeMask;

                // 4) 엣지 영역
                float edgeStart = _OutlineThickness;
                float edgeEnd   = _OutlineThickness + _EdgeThickness;

                float edgeMask = 1.0 - smoothstep(edgeStart, edgeEnd, insideDist);
                edgeMask = saturate(edgeMask) * shapeMask;
                edgeMask *= _EdgeGradient;

                // 5) 기본 컬러
                fixed4 texCol = tex2D(_MainTex, uv);
                fixed4 col = texCol * _FillColor;

                // 6) 전체 페이드
                float fadeMask = ComputeFadeMask(uv);
                col.a *= shapeMask * fadeMask;

                // 7) 엣지 그라데이션
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, edgeMask);

                // 8) 윤곽선 색
                col.rgb = lerp(col.rgb, _OutlineColor.rgb, outlineMask);
                col.a   = max(col.a, _OutlineColor.a * outlineMask);

                // 9) 버텍스 컬러
                col *= i.color;

                // 10) 모양 밖 완전 투명
                col.a *= shapeMask;

                // ===== 11) RectMask2D + Softness 적용 =====
                #ifdef UNITY_UI_CLIP_RECT
                    half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                    col.a *= m.x * m.y;
                #endif

                // ===== 12) Alpha Clip (선택) =====
                #ifdef UNITY_UI_ALPHACLIP
                    clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
