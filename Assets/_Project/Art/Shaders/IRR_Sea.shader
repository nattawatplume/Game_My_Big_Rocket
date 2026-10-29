// Island Rocket Rumble - mint toon sea with gentle vertex waves and lighter foam bands.
Shader "IRR/Sea"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.65, 0.86, 0.8, 1)
        _DeepColor ("Trough Color", Color) = (0.5, 0.78, 0.7, 1)
        _FoamColor ("Foam Color", Color) = (0.93, 1, 0.97, 1)
        _WaveHeight ("Wave Height", Range(0, 1)) = 0.12
        _WaveLength ("Wave Length", Range(0.5, 30)) = 7
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 0.8
        _BandScale ("Foam Band Scale", Range(0.01, 2)) = 0.35
        _BandWidth ("Foam Band Width", Range(0, 1)) = 0.08
        _ShadeThreshold ("Shade Threshold", Range(0, 1)) = 0.45
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _DeepColor;
            float4 _FoamColor;
            float _WaveHeight;
            float _WaveLength;
            float _WaveSpeed;
            float _BandScale;
            float _BandWidth;
            float _ShadeThreshold;
            float _ShadowStrength;
        CBUFFER_END

        // Sum of two sines in world XZ. Returns height in x, d/dx in y, d/dz in z.
        float3 SeaWave(float2 p)
        {
            float t = _Time.y * _WaveSpeed;
            float k1 = 6.2831853 / _WaveLength;
            float k2 = k1 * 1.7;
            float2 d1 = float2(0.8, 0.6);
            float2 d2 = float2(-0.45, 0.89);
            float a1 = _WaveHeight;
            float a2 = _WaveHeight * 0.45;
            float ph1 = dot(d1, p) * k1 + t;
            float ph2 = dot(d2, p) * k2 + t * 1.3;
            float h = a1 * sin(ph1) + a2 * sin(ph2);
            float2 grad = a1 * k1 * cos(ph1) * d1 + a2 * k2 * cos(ph2) * d2;
            return float3(h, grad.x, grad.y);
        }

        float3 DisplaceWS(float3 positionWS, out float3 normalWS, out float height)
        {
            float3 w = SeaWave(positionWS.xz);
            height = w.x;
            normalWS = normalize(float3(-w.y, 1.0, -w.z));
            return positionWS + float3(0, w.x, 0);
        }
        ENDHLSL

        Pass
        {
            Name "SeaForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex SeaVert
            #pragma fragment SeaFrag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float height : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings SeaVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS;
                float height;
                positionWS = DisplaceWS(positionWS, normalWS, height);

                output.positionWS = positionWS;
                output.normalWS = normalWS;
                output.height = height;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float4 SeaFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 normalWS = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // Crest/trough tint from wave height.
                float h01 = saturate(input.height / max(_WaveHeight * 1.45, 0.0001) * 0.5 + 0.5);
                float3 color = lerp(_DeepColor.rgb, _BaseColor.rgb, smoothstep(0.2, 0.8, h01));

                // Toon step from the light + received shadows.
                float halfLambert = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
                float lit = smoothstep(_ShadeThreshold - 0.05, _ShadeThreshold + 0.05, halfLambert);
                lit = min(lit, smoothstep(0.3, 0.7, mainLight.shadowAttenuation));
                color *= lerp(1.0 - _ShadowStrength, 1.0, lit);

                // Lighter drifting bands near the crests.
                float2 p = input.positionWS.xz * _BandScale;
                float band = sin(p.x * 1.3 + p.y * 0.7 + _Time.y * 0.6) * 0.5 + 0.5;
                band *= sin(p.x * -0.6 + p.y * 1.1 - _Time.y * 0.45) * 0.5 + 0.5;
                float foam = smoothstep(1.0 - _BandWidth, 1.0 - _BandWidth * 0.5, band);
                foam = max(foam, smoothstep(0.9, 0.97, h01) * 0.6);
                color = lerp(color, _FoamColor.rgb, foam * 0.75);

                color += _BaseColor.rgb * SampleSH(normalWS) * 0.15;
                color = MixFog(color, input.fogFactor);
                return float4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex SeaDepthVert
            #pragma fragment SeaDepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings SeaDepthVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS;
                float height;
                positionWS = DisplaceWS(positionWS, normalWS, height);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half SeaDepthFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
