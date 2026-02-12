Shader "Universal Render Pipeline/Skybox/SimpleGradientSkybox"
{
    Properties
    {
        _Tint ("Tint Color", Color) = (0.5, 0.5, 0.5, 1)
        _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0
        [NoScaleOffset] _Tex ("Cubemap (HDR)", Cube) = "grey" {}
        
        [Header(Gradient Fallback)]
        _TopColor ("Top Color", Color) = (0.3, 0.4, 0.6, 1)
        _MiddleColor ("Middle Color", Color) = (0.1, 0.1, 0.1, 1)
        _BottomColor ("Bottom Color", Color) = (0.05, 0.05, 0.05, 1)
        _GradientSpread ("Gradient Spread", Range(0.1, 20)) = 1.0
        
        [Toggle] _UseTexture ("Use Cubemap Texture", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
        LOD 100

        Pass
        {
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Exposure;
                float _Rotation;
                half4 _TopColor;
                half4 _MiddleColor;
                half4 _BottomColor;
                float _GradientSpread;
                float _UseTexture;
            CBUFFER_END

            TEXTURECUBE(_Tex);
            SAMPLER(sampler_Tex);

            float3 RotateAroundYInDegrees (float3 vertex, float degrees)
            {
                float alpha = degrees * PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = input.positionOS.xyz;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float3 viewDir = normalize(input.texcoord);
                float3 rotatedViewDir = RotateAroundYInDegrees(viewDir, _Rotation);

                half3 finalColor = half3(0,0,0);

                if (_UseTexture > 0.5)
                {
                    half4 tex = SAMPLE_TEXTURECUBE(_Tex, sampler_Tex, rotatedViewDir);
                    finalColor = tex.rgb * _Tint.rgb * _Exposure;
                }
                else
                {
                    // Gradient calculation
                    float p = normalize(input.texcoord).y;
                    float p1 = 1.0f - pow(min(1.0f, 1.0f - p), _GradientSpread);
                    float p3 = 1.0f - pow(min(1.0f, 1.0f + p), _GradientSpread);
                    float p2 = 1.0f - p1 - p3;

                    half3 gradientColor = (_TopColor.rgb * p1 + _MiddleColor.rgb * p2 + _BottomColor.rgb * p3) * _Exposure;
                    finalColor = gradientColor;
                }

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Skybox/6 Sided"
}
