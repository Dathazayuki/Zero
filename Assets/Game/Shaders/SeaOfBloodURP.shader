Shader "Zero/Sea Of Blood URP"
{
    Properties
    {
        _BaseColor("Crimson", Color) = (0.28, 0.012, 0.025, 1)
        _DeepColor("Burgundy", Color) = (0.06, 0.003, 0.012, 1)
        _NoiseTex("Noise (Repeat)", 2D) = "gray" {}
        _NormalMap("Normal (Repeat)", 2D) = "bump" {}
        _Tiling("World Tiling", Float) = 0.4
        _Speed("Flow Speed", Float) = 0.06
        _Distortion("UV Distortion", Range(0,0.1)) = 0.025
        _NormalStrength("Normal Strength", Range(0,1)) = 0.25
        _Fresnel("Fresnel", Range(0,1)) = 0.15
        _FloodProgress("Flood Progress", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "BloodSurface"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _DeepColor;
                float _Tiling, _Speed, _Distortion, _NormalStrength, _Fresnel, _FloodProgress;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float2 localXZ : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.localXZ = input.positionOS.xz * float2(length(GetObjectToWorldMatrix()._m00_m10_m20), length(GetObjectToWorldMatrix()._m02_m12_m22));
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.localXZ * _Tiling;
                float2 flow = _Time.y * _Speed * float2(0.8, 0.43);
                float n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv + flow).r;
                float2 uv2 = uv * 1.37 - flow.yx + (n1 - 0.5) * _Distortion;
                float n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv2).r;
                float3 a = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv + flow));
                float3 b = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2));
                float3 normalWS = normalize(float3((a.x + b.x) * _NormalStrength, 1, (a.y + b.y) * _NormalStrength));
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float rim = pow(1 - saturate(abs(dot(normalWS, viewDir))), 3) * _Fresnel;
                Light mainLight = GetMainLight();
                half3 illumination = max(SampleSH(normalWS), half3(0.12, 0.12, 0.12)) + mainLight.color * saturate(dot(normalWS, mainLight.direction));
                half3 color = lerp(_DeepColor.rgb, _BaseColor.rgb, saturate(0.35 + (n1+n2)*0.3 - _FloodProgress*0.1));
                return half4(color * illumination + _BaseColor.rgb * rim, 1);
            }
            ENDHLSL
        }
    }
}
