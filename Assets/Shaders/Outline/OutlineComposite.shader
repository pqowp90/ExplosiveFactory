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
            #pragma vertex vert
            #pragma fragment frag

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

            float4 _MaskTex_TexelSize;
            float4 _OutlineColor;
            float _OutlineWidth;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 texel = _MaskTex_TexelSize.xy;
                if (texel.x <= 0.00001 || texel.y <= 0.00001)
                {
                    texel = float2(1.0 / max(1.0, _ScreenParams.x), 1.0 / max(1.0, _ScreenParams.y));
                }
                texel *= max(1.0, _OutlineWidth);

                // Center mask value
                float centerMask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv).r;

                // Sample 8 neighbors
                float up = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(0, texel.y)).r;
                float down = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv - float2(0, texel.y)).r;
                float left = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv - float2(texel.x, 0)).r;
                float right = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(texel.x, 0)).r;

                float upLeft = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(-texel.x, texel.y)).r;
                float upRight = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(texel.x, texel.y)).r;
                float downLeft = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(-texel.x, -texel.y)).r;
                float downRight = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(texel.x, -texel.y)).r;

                float maxNeighbor = max(max(max(up, down), max(left, right)), max(max(upLeft, upRight), max(downLeft, downRight)));

                // Edge is where neighbors are filled but center is not
                float edge = saturate(maxNeighbor - centerMask);

                if (edge <= 0.001)
                {
                    discard;
                }

                return half4(_OutlineColor.rgb, _OutlineColor.a * edge);
            }
            ENDHLSL
        }
    }
}
