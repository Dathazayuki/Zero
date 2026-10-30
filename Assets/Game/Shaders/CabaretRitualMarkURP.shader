Shader "Zero/Cabaret Ritual Mark"
{
    Properties
    {
        _BaseMap ("Eye image", 2D) = "white" {}
        [HDR] _BaseColor ("Colour", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _Mode ("Eye / disc / beam", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Opacity;
                float _Mode;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).r;
                if (_Mode > .5 && _Mode < 1.5)
                {
                    float radius = length(input.uv * 2 - 1);
                    alpha = (1 - smoothstep(.90, 1, radius)) * (.25 + .75 * smoothstep(.65, .88, radius));
                }
                if (_Mode > 1.5) alpha = .5 * (1 - input.uv.y) + .05;
                return half4(_BaseColor.rgb, alpha * _Opacity * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
