// Island Rocket Rumble - flat translucent color for light beams, warning rings and scorch marks.
// Optional vertical fade (alpha falls off toward uv.y = 1) for light beams.
Shader "IRR/Unlit Transparent"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 0.6)
        [ToggleUI] _VerticalFade ("Vertical Fade (0/1)", Float) = 0
        _FadeStart ("Fade Start (uv.y)", Range(0, 1)) = 0.1
        _FadePower ("Fade Power", Range(0.1, 8)) = 1.5
        _EdgeBoost ("Bottom Glow", Range(0, 2)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _VerticalFade;
            float _FadeStart;
            float _FadePower;
            float _EdgeBoost;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "UnlitTransparent"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex UnlitVert
            #pragma fragment UnlitFrag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings UnlitVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float4 UnlitFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 col = _BaseColor;
                if (_VerticalFade > 0.5)
                {
                    float t = saturate((input.uv.y - _FadeStart) / max(1.0 - _FadeStart, 0.001));
                    col.a *= pow(saturate(1.0 - t), _FadePower);
                    // A slightly brighter base where the beam meets the ground.
                    col.rgb += col.rgb * _EdgeBoost * saturate(1.0 - input.uv.y * 4.0);
                }
                col.rgb = MixFog(col.rgb, input.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
