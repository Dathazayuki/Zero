Shader "Zero/Cabaret Glitch Space"
{
    Properties
    {
        _Intensity ("Glitch intensity", Range(0,1)) = 0
        _OverlayMode ("Screen overlay", Float) = 0
        _Observer ("Player camera position", Vector) = (0,0,0,0)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite Off
            ZTest [_ZTest]
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Intensity, _OverlayMode, _ZTest;
                float4 _Observer;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            float Hash(float2 p)
            {
                float3 q = frac(float3(p.xyx) * .1031);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float tick = fmod(floor(_Time.y * 14), 1024);
                float band = floor(input.positionOS.y * 65);
                input.positionOS.x += (1 - _OverlayMode) * (Hash(float2(band,tick)) - .5) * .08;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                clip(_Intensity - .001);
                clip(lerp(1, .05 - distance(_WorldSpaceCameraPos, _Observer.xyz), _OverlayMode));
                float tick = fmod(floor(_Time.y * 14), 1024);
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float row = floor(uv.y * 95);
                float tear = Hash(float2(row, tick));
                float shift = (tear - .5) * .09 * step(.56, tear);
                float2 warped = saturate(uv + float2(shift, (Hash(float2(tick, row)) - .5) * .009));
                float3 scene = SampleSceneColor(warped);
                scene.r = SampleSceneColor(saturate(warped + float2(.006, 0))).r;
                scene.b = SampleSceneColor(saturate(warped - float2(.006, 0))).b;
                float grain = Hash(floor(uv * _ScaledScreenParams.xy * .5) + tick * 17);
                float broken = step(.78, Hash(float2(floor(uv.x * 19), row) + tick));
                float scan = step(.965, frac(uv.y * _ScaledScreenParams.y * .5));
                float3 screen = lerp(scene, (.008 + grain * .18).xxx, broken * .8);
                screen += scan * .08;
                float stripe = Hash(float2(floor(input.positionWS.y * 22), tick));
                float3 shell = lerp(scene * .22, (.002 + grain * grain * .12).xxx, step(.28, stripe));
                shell += step(.96, stripe) * .45;
                float alpha = lerp(.88, _Intensity, _OverlayMode);
                return half4(lerp(shell, screen, _OverlayMode), alpha);
            }
            ENDHLSL
        }
    }
}
