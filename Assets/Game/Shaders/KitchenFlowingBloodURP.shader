Shader "Zero/Kitchen Flowing Blood URP"
{
    Properties
    {
        _BaseColor("Blood Color", Color) = (0.16, 0.003, 0.008, 1)
        _Smoothness("Wet Smoothness", Range(0,1)) = 0.94
        _Mode("0 Surface, 1 Stream, 2 Overflow, 3 Pool", Float) = 0
        _FlowSpeed("Flow Speed", Float) = 1
        _NormalStrength("Moving Detail", Range(0,1)) = 0.15
        _RippleCenter("Impact UV", Vector) = (0.33,0.7,0,0)
        _Opacity("Opacity", Range(0,1)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", Float) = 0
        [Toggle] _ZWrite("Depth Write", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+5" }
        Pass
        {
            Name "WetBlood"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _RippleCenter;
                float _Smoothness, _Mode, _FlowSpeed, _NormalStrength, _Opacity;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fog : TEXCOORD4;
                half3 vertexLight : TEXCOORD5;
            };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                float t = _Time.y * _FlowSpeed;
                float oscillation = sin(input.uv.y * 65 - t * 22) * sin(input.uv.x * 19 + t * 6);
                float3 p = input.positionOS.xyz;
                p += input.normalOS * oscillation * (_Mode > 0.5 && _Mode < 2.5 ? 0.0018 : 0.00045);
                VertexPositionInputs positions = GetVertexPositionInputs(p);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                o.positionCS = positions.positionCS; o.positionWS = positions.positionWS;
                o.normalWS = normals.normalWS;
                o.tangentWS = half4(normals.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                o.uv = input.uv; o.fog = ComputeFogFactor(o.positionCS.z);
                o.vertexLight = VertexLighting(positions.positionWS, normals.normalWS);
                return o;
            }
            half4 Frag(Varyings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float t = _Time.y * _FlowSpeed;
                float2 uv = input.uv;
                float waveA = sin(uv.x * 47 + uv.y * 31 - t * 19);
                float waveB = cos(uv.x * 27 - uv.y * 53 + t * 13);
                float2 delta = uv - _RippleCenter.xy;
                float radius = length(delta);
                float ripple = cos(radius * 75 - t * 12) * exp(-radius * 5);
                float surface = _Mode < 0.5 || _Mode > 2.5 ? 1 : 0;
                float2 perturb = float2(waveA, waveB) * _NormalStrength * 0.22;
                perturb += normalize(delta + 0.0001) * ripple * surface * 0.022;
                half3 normal = normalize(input.normalWS) * IS_FRONT_VFACE(facing, 1, -1);
                half3 tangent = normalize(input.tangentWS.xyz);
                half3 bitangent = normalize(cross(normal, tangent)) * input.tangentWS.w;
                normal = normalize(normal + tangent * perturb.x + bitangent * perturb.y);
                InputData data = (InputData)0;
                data.positionWS = input.positionWS;
                data.normalWS = normal;
                data.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                data.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                data.bakedGI = SampleSH(normal);
                data.vertexLighting = input.vertexLight;
                data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                data.shadowMask = half4(1,1,1,1);
                SurfaceData material = (SurfaceData)0;
                material.albedo = _BaseColor.rgb;
                material.metallic = 0;
                material.specular = half3(0.04,0.04,0.04);
                material.smoothness = _Smoothness;
                material.normalTS = half3(0,0,1);
                material.occlusion = 1;
                material.alpha = _Opacity;
                half4 color = UniversalFragmentPBR(data, material);
                color.rgb = MixFog(color.rgb, input.fog);
                color.a = _Opacity;
                return color;
            }
            ENDHLSL
        }
    }
}
