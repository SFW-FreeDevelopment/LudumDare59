Shader "SignalScrubber/CRT"
{
    Properties
    {
        [NoScaleOffset] _HiddenImage ("Hidden Image", 2D) = "black" {}
        [NoScaleOffset] _NoiseTex    ("Noise Texture", 2D) = "white" {}
        _Reveal        ("Reveal",         Range(0, 1)) = 0.0
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 1.0
        _Chromatic     ("Chromatic",      Range(0, 1)) = 0.5
        _Rolling       ("Rolling",        Range(0, 1)) = 0.5
        _Ghost         ("Ghost",          Range(0, 1)) = 0.3
        _Scanlines     ("Scanlines",      Float) = 240.0
        _Curvature     ("Curvature",      Range(0, 1)) = 0.15
        _Flicker       ("Flicker",        Range(0, 1)) = 0.2
        _NoiseScroll   ("Noise Scroll",   Vector) = (0.3, 0.9, 0, 0)
        _Tint          ("Tint",           Color)  = (0.49, 1.0, 0.62, 1.0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }
        LOD 100

        Pass
        {
            Name "CRT"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_HiddenImage); SAMPLER(sampler_HiddenImage);
            TEXTURE2D(_NoiseTex);    SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float  _Reveal;
                float  _NoiseStrength;
                float  _Chromatic;
                float  _Rolling;
                float  _Ghost;
                float  _Scanlines;
                float  _Curvature;
                float  _Flicker;
                float4 _NoiseScroll;
                float4 _Tint;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float2 Barrel(float2 uv, float k)
            {
                float2 c = uv - 0.5;
                float r2 = dot(c, c);
                return uv + c * r2 * k * 4.0;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Barrel distortion of sampling UVs. Pixels past the edge
                // are discarded so the screen appears "rounded" inside the
                // quad rather than wrapping.
                float2 uv = Barrel(i.uv, _Curvature);
                float inside = step(0.0, uv.x) * step(uv.x, 1.0)
                             * step(0.0, uv.y) * step(uv.y, 1.0);

                // Rolling vertical offset — subtle continuous slide.
                float roll = sin(_Time.y * 1.3 + uv.y * 6.283) * 0.02 * _Rolling;
                uv.y += roll;

                // Chromatic aberration via per-channel horizontal offset.
                float c = _Chromatic * 0.01;
                half r = SAMPLE_TEXTURE2D(_HiddenImage, sampler_HiddenImage, uv + float2( c, 0)).r;
                half g = SAMPLE_TEXTURE2D(_HiddenImage, sampler_HiddenImage, uv                      ).g;
                half b = SAMPLE_TEXTURE2D(_HiddenImage, sampler_HiddenImage, uv + float2(-c, 0)).b;
                half3 hidden = half3(r, g, b);

                // Animated noise layer.
                float2 nuv = uv + _Time.y * _NoiseScroll.xy;
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nuv).r;
                half3 noiseCol = half3(noise, noise, noise);

                // Blend hidden image toward pure noise as reveal drops.
                float hide = saturate(1.0 - _Reveal) * _NoiseStrength;
                half3 col = lerp(hidden, noiseCol, hide);

                // Ghost / doubled image trailing to the right.
                half3 ghost = SAMPLE_TEXTURE2D(_HiddenImage, sampler_HiddenImage,
                                               uv + float2(_Ghost * 0.015, 0)).rgb;
                col += ghost * 0.4 * _Ghost;

                // Scanlines — every other horizontal band at 65% brightness.
                float scan = 1.0 - step(frac(uv.y * _Scanlines), 0.5) * 0.35;
                col *= scan;

                // Time-noise flicker.
                float f = 1.0 - _Flicker * 0.1 * Hash(float2(_Time.y, 0));
                col *= f;

                // Phosphor tint.
                col *= _Tint.rgb;

                // Darken outside-curvature pixels to near-black so the screen
                // reads as rounded inside the rectangular bezel.
                col *= inside;

                return half4(col, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
