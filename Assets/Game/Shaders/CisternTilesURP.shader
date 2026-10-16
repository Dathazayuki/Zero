Shader "Zero/Cistern Tiles URP"
{
    Properties
    {
        _BaseColor("Porcelain", Color) = (0.68,0.68,0.65,1)
        _GroutColor("Grout", Color) = (0.19,0.20,0.19,1)
        _TileSize("Tile Size (Metres)", Float) = 0.28
        _GroutWidth("Grout Width", Range(0.005,0.1)) = 0.018
        _Smoothness("Wet Glaze", Range(0,1)) = 0.6
        _AmbientStrength("Ambient Strength", Range(0,1)) = 0.035
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor, _GroutColor;
            float _TileSize, _GroutWidth, _Smoothness, _AmbientStrength;
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
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
        half4 DepthFrag(Varyings input) : SV_Target { return 0; }
        ENDHLSL
        Pass
        {
            Name "Tiles"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float TileMask(float2 coordinate)
            {
                float2 distanceToEdge = 0.5 - abs(frac(coordinate) - 0.5);
                float2 aa = max(fwidth(coordinate), 0.001);
                float2 mask = smoothstep(_GroutWidth, _GroutWidth + aa, distanceToEdge);
                return mask.x * mask.y;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 weights = pow(abs(normal), 8);
                weights /= max(dot(weights, float3(1,1,1)), 0.001);
                float3 p = input.positionWS / max(_TileSize, 0.01);
                float tile = dot(weights, float3(TileMask(p.yz), TileMask(p.xz), TileMask(p.xy)));
                float mottling = 0.96 + 0.04 * sin(p.x * 0.28 + p.y * 0.37) * sin(p.z * 0.41);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = lerp(_GroutColor.rgb, _BaseColor.rgb * mottling, tile);
                surface.alpha = 1;
                surface.occlusion = 1;
                surface.smoothness = _Smoothness * tile;
                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.normalWS = normal;
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.bakedGI = SampleSH(normal) * _AmbientStrength;
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1,1,1,1);
                half4 color = UniversalFragmentPBR(lighting, surface);
                color.rgb = MixFog(color.rgb, ComputeFogFactor(input.positionCS.z));
                return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float3 _LightDirection, _LightPosition;
            Varyings ShadowVert(Attributes input)
            {
                Varyings output = Vert(input);
                float3 lightDir = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    lightDir = normalize(_LightPosition - output.positionWS);
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS, output.normalWS, lightDir));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }
            ENDHLSL
        }
    }
}
