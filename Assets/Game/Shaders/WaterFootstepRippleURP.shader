Shader "Zero/Water Footstep Ripple URP"
{
    Properties
    {
        _Color("Ripple Tint", Color) = (0.42,0.46,0.45,0.32)
        _Progress("Progress", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Progress;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv - 0.5;
                float distance = length(p);
                float radius = lerp(0.04, 0.46, _Progress);
                float width = max(fwidth(distance) * 1.5, 0.008);
                float outer = 1 - smoothstep(width, width * 2.5, abs(distance - radius));
                float inner = 1 - smoothstep(width, width * 2.5, abs(distance - radius * 0.73));
                float fade = smoothstep(0, 0.08, _Progress) * (1 - _Progress) * (1 - _Progress);
                float broken = 0.8 + 0.2 * sin(p.x * 73 + p.y * 41 + _Progress * 5);
                float alpha = saturate(outer + inner * 0.4) * fade * broken * _Color.a;
                return half4(MixFog(_Color.rgb, input.fog), alpha);
            }
            ENDHLSL
        }
    }
}
