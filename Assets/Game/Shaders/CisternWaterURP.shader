Shader "Zero/Cistern Water URP"
{
    Properties
    {
        _WaterColor("Water Tint", Color) = (0.025,0.03,0.032,1)
        _WaveScale("Ripple Scale", Float) = 3.0
        _WaveSpeed("Ripple Speed", Float) = 0.28
        _WaveStrength("Ripple Strength", Range(0,0.3)) = 0.05
        _Absorption("Depth Absorption", Range(0,12)) = 2.5
        _ReflectionStrength("Reflection Strength", Range(0,2)) = 1.0
        _Refraction("Refraction", Range(0,0.04)) = 0.004
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _WaterColor;
                float _WaveScale, _WaveSpeed, _WaveStrength, _Absorption;
                float _ReflectionStrength, _Refraction;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float eyeDepth : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.eyeDepth = -TransformWorldToView(output.positionWS).z;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz * _WaveScale;
                float t = _Time.y * _WaveSpeed;
                float2 slope = float2(cos(p.x + p.y * 0.63 + t), cos(p.y - p.x * 0.71 - t * 0.73));
                slope += 0.35 * float2(sin(p.y * 2.3 + t), sin(p.x * 2.7 - t));
                float3 normal = normalize(float3(slope.x * _WaveStrength, 1, slope.y * _WaveStrength));
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float depth = max(0, sceneDepth - input.eyeDepth) * abs(view.y);
                float2 distortedUV = saturate(screenUV + slope * _Refraction * saturate(depth));
                half3 transmitted = lerp(SampleSceneColor(distortedUV), _WaterColor.rgb, 1 - exp(-depth * _Absorption));
                half3 reflected = GlossyEnvironmentReflection(reflect(-view, normal), input.positionWS, 0.08, 1, screenUV);
                float fresnel = 0.035 + 0.965 * pow(1 - saturate(dot(normal, view)), 5);
                half3 color = lerp(transmitted, reflected * _ReflectionStrength, fresnel);
                #if defined(_ADDITIONAL_LIGHTS) || USE_FORWARD_PLUS
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = screenUV;
                    uint lightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(lightCount)
                        Light light = GetAdditionalLight(lightIndex, input.positionWS);
                        float3 halfway = normalize(view + light.direction);
                        color += light.color * pow(saturate(dot(normal, halfway)), 160) * light.distanceAttenuation * 0.5;
                    LIGHT_LOOP_END
                #endif
                return half4(MixFog(color, ComputeFogFactor(input.positionCS.z)), 1);
            }
            ENDHLSL
        }
    }
}
