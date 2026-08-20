Shader "Hidden/OutlineComposite"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
        _MaskTex ("Mask Tex", 2D) = "black" {}
        _OutlineColor ("Outline Color", Color) = (1, 0.92, 0.016, 1)
        _OutlineWidth ("Outline Width", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "OutlineComposite"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float4 _MaskTex_TexelSize;
            float4 _OutlineColor;
            float _OutlineWidth;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float SampleMask(float2 uv)
            {
                float m1 = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv).r;
                float m2 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).r;
                return max(m1, m2);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 texel = _MaskTex_TexelSize.xy;
                if (texel.x <= 0.000001 || texel.y <= 0.000001)
                {
                    texel = float2(1.0 / max(1.0, _ScreenParams.x), 1.0 / max(1.0, _ScreenParams.y));
                }

                // Center mask value
                float centerMask = SampleMask(uv);

                // 원형 다중 샘플링 (16방향 2단계 링)으로 모서리 각짐을 없애고 부드러운 라운딩 적용
                float width = max(1.0, _OutlineWidth);
                float maxNeighbor = 0.0;
                float sampleAccum = 0.0;

                static const int DIRECTIONS = 16;
                static const float ANGLE_STEP = 6.28318530718 / 16.0;

                [unroll]
                for (int i = 0; i < DIRECTIONS; i++)
                {
                    float angle = i * ANGLE_STEP;
                    float2 dir = float2(cos(angle), sin(angle)) * texel;

                    float valOuter = SampleMask(uv + dir * width);
                    float valInner = SampleMask(uv + dir * (width * 0.5));

                    maxNeighbor = max(maxNeighbor, max(valOuter, valInner));
                    sampleAccum += valOuter + valInner;
                }

                // Edge is where neighbors are filled but center is not
                float edge = saturate(maxNeighbor - centerMask);

                if (edge <= 0.001)
                {
                    discard;
                }

                // 안티앨리어싱 스무스 엣지 블렌딩
                float smoothFactor = saturate(sampleAccum / (DIRECTIONS * 1.2));
                float finalAlpha = _OutlineColor.a * lerp(edge, 1.0, smoothFactor * 0.5);

                return half4(_OutlineColor.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
