Shader "Zero/Cabaret Black Hole"
{
    Properties { _Open("Open", Range(0,1)) = 0 _Pulse("Pulse", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float _Open, _Pulse;
            CBUFFER_END
            V Vert(A i) { V o; o.p=TransformObjectToHClip(i.p.xyz); o.uv=i.uv; return o; }
            half4 Frag(V i):SV_Target
            {
                float2 p=(i.uv-.5)*2;
                float r=length(p), angle=atan2(p.y,p.x);
                float spiral=sin(angle*6+r*28-_Time.y*7)*.5+.5;
                float edge=exp(-abs(r-.84)*40);
                float alpha=(1-smoothstep(.90,.99,r))*lerp(1,smoothstep(.76,.83,r),_Open);
                half3 glow=half3(.36,.025,.012)*edge*(.65+spiral*.5+_Pulse*.25);
                return half4(glow,alpha);
            }
            ENDHLSL
        }
    }
}
