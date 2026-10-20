Shader "Zero/IR2/RugDarkening"
{
    Properties
    {
        _Progress("Progress", Range(0,1)) = 0
        _Target("Feet UV", Vector) = (0.5,0.5,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -1,-1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            float _Progress;
            float4 _Target;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 delta = input.uv - _Target.xy;
                float2 span = max(0.01, lerp(_Target.xy, 1.0 - _Target.xy, step(0.0, delta)));
                float2 ratio = abs(delta) / span;
                float edge = 1.0 - max(ratio.x, ratio.y);
                float mask = max(smoothstep(edge - 0.06, edge + 0.06, _Progress), pow(_Progress, 8));
                return half4(0.008, 0.006, 0.004, saturate(mask) * 0.9 * step(0.001, _Progress));
            }
            ENDHLSL
        }
    }
}
