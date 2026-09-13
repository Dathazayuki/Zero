Shader "Zero/Planar Mirror URP"
{
    Properties
    {
        [MainTexture] _BaseMap("Reflection Texture", 2D) = "black" {}
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _UVRotationDegrees("UV Rotation", Range(-180, 180)) = 0
        [Toggle] _FlipX("Flip X", Float) = 0
        [Toggle] _FlipY("Flip Y", Float) = 0
        [Toggle] _UseMeshUV("Use Mesh UV (Legacy)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PlanarMirror"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPosition : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _UVRotationDegrees;
                float _FlipX;
                float _FlipY;
                float _UseMeshUV;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUv = input.screenPosition.xy / input.screenPosition.w;
                #if UNITY_UV_STARTS_AT_TOP
                    if (_BaseMap_TexelSize.y < 0.0)
                    {
                        screenUv.y = 1.0 - screenUv.y;
                    }
                #endif

                float2 uv = lerp(screenUv, input.uv, saturate(_UseMeshUV));
                float2 centered = uv - 0.5;
                float radiansValue = radians(_UVRotationDegrees);
                float sineValue;
                float cosineValue;
                sincos(radiansValue, sineValue, cosineValue);
                centered = float2(
                    centered.x * cosineValue - centered.y * sineValue,
                    centered.x * sineValue + centered.y * cosineValue);
                uv = centered + 0.5;

                if (_FlipX > 0.5)
                {
                    uv.x = 1.0 - uv.x;
                }

                if (_FlipY > 0.5)
                {
                    uv.y = 1.0 - uv.y;
                }

                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
