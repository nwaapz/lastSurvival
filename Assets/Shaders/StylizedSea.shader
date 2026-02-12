Shader "Custom/StylizedSea"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.10, 0.75, 0.85, 1)
        _DeepColor    ("Deep Color",    Color) = (0.02, 0.18, 0.35, 1)
        _FoamColor    ("Foam Color",    Color) = (1, 1, 1, 1)

        _Opacity      ("Opacity", Range(0,1)) = 0.85
        _DepthMax     ("Depth Max (m)", Range(0.1, 50)) = 6

        _WaveDir1     ("Wave Dir 1 (x,z)", Vector) = (1, 0, 0, 0)
        _WaveAmp1     ("Wave Amp 1", Range(0, 2)) = 0.25
        _WaveFreq1    ("Wave Freq 1", Range(0.01, 10)) = 1.2
        _WaveSpeed1   ("Wave Speed 1", Range(-10, 10)) = 1.2

        _WaveDir2     ("Wave Dir 2 (x,z)", Vector) = (0.3, 0, 1, 0)
        _WaveAmp2     ("Wave Amp 2", Range(0, 2)) = 0.12
        _WaveFreq2    ("Wave Freq 2", Range(0.01, 10)) = 2.2
        _WaveSpeed2   ("Wave Speed 2", Range(-10, 10)) = 1.8

        _FoamShoreMax   ("Shore Foam Width (m)", Range(0.01, 3)) = 0.6
        _FoamCrestStart ("Crest Start Height", Range(-1, 1)) = 0.10
        _FoamCrestRange ("Crest Range", Range(0.001, 1)) = 0.25
        _FoamNoiseScale ("Foam Noise Scale", Range(0.1, 10)) = 2.0
        _FoamNoiseSpeed ("Foam Noise Speed", Range(0, 10)) = 0.7
        _FoamIntensity  ("Foam Intensity", Range(0, 3)) = 1.2

        _LightSteps    ("Toon Light Steps", Range(1, 8)) = 4
        _FresnelPower  ("Fresnel Power", Range(0.1, 8)) = 3
        _SpecPower     ("Specular Power", Range(1, 256)) = 64
        _SpecIntensity ("Spec Intensity", Range(0, 5)) = 1.1

        // URP specific
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP Keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float _Opacity;
                float _DepthMax;

                float4 _WaveDir1;
                float _WaveAmp1, _WaveFreq1, _WaveSpeed1;
                float4 _WaveDir2;
                float _WaveAmp2, _WaveFreq2, _WaveSpeed2;

                float _FoamShoreMax, _FoamCrestStart, _FoamCrestRange;
                float _FoamNoiseScale, _FoamNoiseSpeed, _FoamIntensity;

                float _LightSteps, _FresnelPower, _SpecPower, _SpecIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float  waveH        : TEXCOORD3;
                float4 screenPos    : TEXCOORD4;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            void WaveInfo(float2 xz, float t, out float h, out float dhdx, out float dhdz)
            {
                float2 d1 = normalize(_WaveDir1.xz);
                float2 d2 = normalize(_WaveDir2.xz);

                float p1 = dot(xz, d1) * _WaveFreq1 + t * _WaveSpeed1;
                float p2 = dot(xz, d2) * _WaveFreq2 + t * _WaveSpeed2;

                float s1 = sin(p1);
                float c1 = cos(p1);
                float s2 = sin(p2);
                float c2 = cos(p2);

                h    = s1 * _WaveAmp1 + s2 * _WaveAmp2;
                dhdx = c1 * _WaveAmp1 * _WaveFreq1 * d1.x + c2 * _WaveAmp2 * _WaveFreq2 * d2.x;
                dhdz = c1 * _WaveAmp1 * _WaveFreq1 * d1.y + c2 * _WaveAmp2 * _WaveFreq2 * d2.y;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                float h, dhdx, dhdz;
                WaveInfo(worldPos.xz, _Time.y, h, dhdx, dhdz);
                worldPos.y += h;

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = input.uv;
                output.waveH = h;
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Reconstruct Normal
                float h, dhdx, dhdz;
                WaveInfo(input.positionWS.xz, _Time.y, h, dhdx, dhdz);
                float3 N = normalize(float3(-dhdx, 1.0, -dhdz));

                // View Direction
                float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // Depth Softness
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                // LinearEyeDepth requires correct buffer params
                #if UNITY_REVERSED_Z
                    float rawDepth = SampleSceneDepth(screenUV);
                #else
                    float rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                #endif
                
                float sceneDepthEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                // Correct pixel depth from clip space Z
                float pixelDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                
                // Difference
                float thickness = max(sceneDepthEye - pixelDepth, 0.0);
                float depth01 = saturate(thickness / max(_DepthMax, 1e-4));

                float3 baseCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth01);

                // Lighting
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 L = mainLight.direction;
                float NdotL = saturate(dot(N, L));

                // CEL SHADING STEPS
                float steps = max(_LightSteps, 1.0);
                float lit = floor(NdotL * steps) / steps;
                
                float shadow = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                float3 lightColor = mainLight.color * (0.25 + 0.75 * lit) * shadow;

                float3 ambient = SampleSH(N); // Spherical Harmonics Ambient
                
                float3 diffuse = baseCol * lightColor + baseCol * ambient * 0.5;

                // Specular
                float3 H = normalize(L + V);
                float NdotH = saturate(dot(N, H));
                float spec = pow(NdotH, _SpecPower) * _SpecIntensity * shadow;

                // Fresnel
                float NdotV = saturate(dot(N, V));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                float3 fresCol = fresnel * 0.35 * mainLight.color;

                // FOAM
                float shoreFoam = 1.0 - saturate(thickness / max(_FoamShoreMax, 1e-4));
                shoreFoam = smoothstep(0.0, 1.0, shoreFoam);

                float crestFoam = saturate((input.waveH - _FoamCrestStart) / max(_FoamCrestRange, 1e-4));
                crestFoam = smoothstep(0.0, 1.0, crestFoam);

                float n = noise(input.positionWS.xz * _FoamNoiseScale + _Time.y * _FoamNoiseSpeed);
                float foamMask = (shoreFoam * 0.9 + crestFoam) * saturate(n * 1.2);
                foamMask = saturate(foamMask * _FoamIntensity);

                float3 finalColor = diffuse + spec + fresCol;
                finalColor = lerp(finalColor, _FoamColor.rgb, foamMask);

                // Fog
                float fogFactor = ComputeFogFactor(input.positionCS.z);
                finalColor = MixFog(finalColor, fogFactor);

                return half4(finalColor, _Opacity);
            }
            ENDHLSL
        }
    }
}
