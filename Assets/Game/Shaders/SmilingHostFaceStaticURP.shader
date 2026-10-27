Shader "Zero/Smiling Host Face Static"
{
    Properties
    {
        [HideInInspector] _Face ("Legacy face mask", Vector) = (0,2.006,0.145,0.14)
        [HideInInspector] _Mouth ("Legacy smile cutout", Vector) = (0,1.934,0.077,0.014)
        _Band ("Horizontal band center XY / half size XY", Vector) = (0,2.01,0.16,0.05)
        _FrontCut ("Front surface minimum Z", Float) = 0.055
        _Speed ("Static changes per second", Range(1,40)) = 22
        _Grain ("Grain density", Range(40,1200)) = 800
        _Brightness ("Dark grain brightness", Range(0,0.3)) = 0.085
        _Coverage ("Coverage", Range(0,1)) = 1
        _Tearing ("Horizontal tearing", Range(0,0.1)) = 0.015
        _RibbonMode ("Separate ribbon mesh", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull mode", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest+20" "RenderType"="TransparentCutout" }
        Pass
        {
            Name "FaceStatic"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            ZTest LEqual
            Cull [_Cull]
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Face, _Mouth, _Band;
                float _FrontCut, _Speed, _Grain, _Brightness, _Coverage, _Tearing, _RibbonMode, _Cull;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 facePosition : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 facePosition : TEXCOORD0; };
            float Hash(float2 p)
            {
                float3 q = frac(float3(p.xyx) * .1031);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float tick = fmod(floor(_Time.y * _Speed), 1024);
                float stripe = floor(input.facePosition.y * 220);
                input.positionOS.x += _RibbonMode * _Tearing * (Hash(float2(stripe,tick)) - .5);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.facePosition = input.facePosition;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 p = input.facePosition;
                float tick = fmod(floor(_Time.y * _Speed), 1024);
                float row = floor(p.y * 360);
                float jitter = (Hash(float2(row, tick)) - .5) * (.025 + _Tearing);
                float2 band = p.xy - _Band.xy;
                clip(_Coverage - .001);
                clip(p.z - _FrontCut);
                clip(_Band.z + jitter - abs(band.x));
                float edge = (Hash(float2(floor(p.x * 250), tick)) - .5) * .004;
                clip(_Band.w + edge - abs(band.y));
                float displacement = (Hash(float2(floor(p.y*220),tick)) - .5) * _Tearing;
                float grain = Hash(floor((p.xy + float2(displacement,0)) * float2(_Grain * .18, _Grain)) + float2(tick, tick * 7));
                float scanBand = step(.78, Hash(float2(row, tick)));
                float darkness = .001 + _Brightness * (grain * grain * .5 + scanBand * .2);
                return half4(darkness.xxx, 1);
            }
            ENDHLSL
        }
    }
}
