Shader "Zero/Storage Checker URP"
{
    Properties
    {
        _BaseColor("Ivory Tile", Color) = (0.48,0.45,0.37,1)
        _AlternateColor("Dark Tile", Color) = (0.18,0.20,0.18,1)
        _GroutColor("Grout", Color) = (0.08,0.09,0.08,1)
        _TileSize("Tile Size (Metres)", Float) = 0.45
        _GroutWidth("Grout Width", Range(0.005,0.1)) = 0.015
        _Smoothness("Smoothness", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Name "CheckerFloor"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _AlternateColor, _GroutColor;
                float _TileSize, _GroutWidth, _Smoothness;
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
            half4 Frag(Varyings input) : SV_Target
            {
                float2 coordinate = input.positionWS.xz / max(_TileSize, 0.01);
                float2 cell = floor(coordinate);
                float checker = frac((cell.x + cell.y) * 0.5) * 2;
                float2 edge = min(frac(coordinate), 1 - frac(coordinate));
                float seam = smoothstep(_GroutWidth, _GroutWidth + max(fwidth(coordinate.x), 0.001), min(edge.x, edge.y));
                float variation = 0.87 + 0.13 * frac(sin(dot(cell, float2(12.9898,78.233))) * 43758.5453);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = lerp(_GroutColor.rgb, lerp(_BaseColor.rgb, _AlternateColor.rgb, checker) * variation, seam);
                surface.alpha = 1;
                surface.occlusion = 1;
                surface.smoothness = _Smoothness * seam;
                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.normalWS = normalize(input.normalWS);
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.bakedGI = SampleSH(lighting.normalWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1,1,1,1);
                half4 color = UniversalFragmentPBR(lighting, surface);
                color.rgb = MixFog(color.rgb, ComputeFogFactor(input.positionCS.z));
                return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
