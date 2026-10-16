Shader "Zero/WorldSpace Triplanar URP"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map (Albedo)", 2D) = "white" {}
        [MainColor] _BaseColor("Color Tint", Color) = (1, 1, 1, 1)
        _Tiling("World Tiling (X/Z)", Float) = 0.5
        _TilingY("World Tiling (Y)", Float) = 0.5
        _BlendSharpness("Blend Sharpness", Range(1, 16)) = 4.0
        _Smoothness("Smoothness", Range(0, 1)) = 0.15
        _Metallic("Metallic", Range(0, 1)) = 0.0
        [Toggle] _EnableNormal("Enable Normal Map", Float) = 0
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1.0
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Tiling;
                float _TilingY;
                float _BlendSharpness;
                float _Smoothness;
                float _Metallic;
                float _EnableNormal;
                float _BumpScale;
                half4 _EmissionColor;
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

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 pos = input.positionWS;
                float3 n = normalize(input.normalWS);

                // Triplanar projection UVs based on world coordinates
                float2 uvX = float2(pos.z * _Tiling, pos.y * _TilingY);
                float2 uvY = float2(pos.x * _Tiling, pos.z * _Tiling);
                float2 uvZ = float2(pos.x * _Tiling, pos.y * _TilingY);

                // Weights calculation with sharpness
                float3 weights = pow(abs(n), _BlendSharpness);
                weights /= (weights.x + weights.y + weights.z + 0.00001);

                // Sample texture for all 3 planes
                half4 colX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX);
                half4 colY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY);
                half4 colZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ);
                half4 albedo = (colX * weights.x + colY * weights.y + colZ * weights.z) * _BaseColor;

                float3 normalWS = n;
                if (_EnableNormal > 0.5)
                {
                    half3 normX = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvX), _BumpScale);
                    half3 normY = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvY), _BumpScale);
                    half3 normZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvZ), _BumpScale);

                    normX = float3(0, normX.y, normX.x * sign(n.x));
                    normY = float3(normY.x, 0, normY.y * sign(n.y));
                    normZ = float3(normZ.x * sign(n.z), normZ.y, 0);

                    normalWS = normalize(n + (normX * weights.x + normY * weights.y + normZ * weights.z));
                }

                // Setup Universal PBR input
                InputData inputData = (InputData)0;
                inputData.positionWS = pos;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(pos);
                inputData.shadowCoord = TransformWorldToShadowCoord(pos);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0.04, 0.04, 0.04);
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = _EmissionColor.rgb;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = albedo.a;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        // Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // DepthOnly Pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
