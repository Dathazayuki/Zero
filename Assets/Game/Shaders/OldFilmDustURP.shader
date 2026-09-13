Shader "Zero/Old Film Dust URP"
{
    Properties
    {
        _Intensity("Effect Intensity", Range(0, 1)) = 0.65
        _DustDensity("Dust Density", Range(0, 1)) = 0.055
        _DustOpacity("Dust Opacity", Range(0, 1)) = 0.35
        _DustSize("Dust Radius (pixels at 1080p)", Range(0.5, 8)) = 1.8
        _DustRate("Dust Frames Per Second", Range(1, 30)) = 14
        _ScratchChance("Scratch Chance", Range(0, 1)) = 0.18
        _ScratchOpacity("Scratch Opacity", Range(0, 1)) = 0.22
        _ScratchWidth("Scratch Width (pixels at 1080p)", Range(0.25, 3)) = 0.7
        _ScratchDuration("Scratch Lifetime (seconds)", Range(0.05, 2)) = 0.35
        _Flicker("Brightness Flicker", Range(0, 0.05)) = 0.01
        _Seed("Random Seed", Float) = 17
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            Name "OldFilmDust"
            ZTest Always ZWrite Off Cull Off Blend Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Intensity, _DustDensity, _DustOpacity, _DustSize, _DustRate;
                float _ScratchChance, _ScratchOpacity, _ScratchWidth, _ScratchDuration;
                float _Flicker, _Seed;
            CBUFFER_END

            float FilmHash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                // Screen-space marks: turning the camera never moves them through the world.
                float2 screenSize = max(_ScaledScreenParams.xy, float2(1, 1));
                float2 uv = input.positionCS.xy / screenSize;
                float2 filmSize = float2(1080.0 * screenSize.x / screenSize.y, 1080.0);
                float2 pixel = uv * filmSize;
                float aa = max(0.35, 1080.0 / screenSize.y);
                float frame = floor(_Time.y * max(_DustRate, 1.0));
                float2 seed = float2(_Seed, frame);

                // Sparse cells prevent a uniform grey noise layer from darkening the scene.
                const float cellSize = 128.0;
                float2 cell = floor(pixel / cellSize);
                float2 inCell = frac(pixel / cellSize) * cellSize;
                float enabledDust = step(FilmHash(cell + seed * 11.3), _DustDensity);
                float2 centre = lerp(16.0, cellSize - 16.0,
                    float2(FilmHash(cell + seed + 7.1), FilmHash(cell + seed + 29.2)));
                float radius = _DustSize * lerp(0.45, 1.0, FilmHash(cell + seed + 53.0));
                float2 delta = inCell - centre;
                delta.x *= lerp(0.7, 1.5, FilmHash(cell + seed + 81.2));
                float dust = enabledDust * (1.0 - smoothstep(max(0.0, radius - aa), radius + aa, length(delta)));

                float scratch = 0.0;
                float lifetime = max(_ScratchDuration, 0.05);
                float epoch = floor(_Time.y / lifetime);
                float phase = frac(_Time.y / lifetime);
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    float2 s = float2(epoch + _Seed, i * 31.7);
                    float enabledScratch = step(FilmHash(s + 2.0), _ScratchChance);
                    float x = FilmHash(s + 13.0) * filmSize.x;
                    float y = FilmHash(s + 41.0) * filmSize.y;
                    float halfLength = lerp(25.0, 190.0, FilmHash(s + 67.0));
                    float bend = sin(pixel.y * 0.012 + FilmHash(s + 9.0) * 6.283) * 0.7;
                    float drift = (phase - 0.5) * 2.0;
                    float scratchLine = 1.0 - smoothstep(max(0.0, _ScratchWidth * 0.5 - aa),
                        _ScratchWidth * 0.5 + aa, abs(pixel.x - x - bend - drift));
                    float ends = 1.0 - smoothstep(halfLength * 0.8, halfLength, abs(pixel.y - y));
                    float breaks = step(0.22, FilmHash(float2(floor(pixel.y / 9.0) + epoch, i + _Seed)));
                    float fade = smoothstep(0.0, 0.12, phase) * (1.0 - smoothstep(0.85, 1.0, phase));
                    scratch = max(scratch, scratchLine * ends * breaks * fade * enabledScratch);
                }

                float darken = saturate(dust * _DustOpacity + scratch * _ScratchOpacity) * _Intensity;
                float flicker = (FilmHash(seed + 103.0) * 2.0 - 1.0) * _Flicker * _Intensity;
                return half4(source.rgb * (1.0 - darken) * (1.0 + flicker), source.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
