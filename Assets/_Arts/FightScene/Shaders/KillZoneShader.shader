Shader "Custom/KillZoneShader"
{
    Properties
    {
        _Color ("Zone Color", Color) = (1, 0.3, 0.1, 0.5)
        _EdgeColor ("Edge Color", Color) = (1, 0.8, 0.2, 0.8)
        _Range ("Range", Float) = 10
        _ForwardFalloff ("Forward Falloff", Range(0.1, 3)) = 1.5
        _EdgeWidth ("Edge Width", Range(0, 1)) = 0.1
        _PulseSpeed ("Pulse Speed", Float) = 2
        _PulseIntensity ("Pulse Intensity", Range(0, 0.5)) = 0.15
        _GridScale ("Grid Scale", Float) = 2
        _GridIntensity ("Grid Intensity", Range(0, 1)) = 0.3
        _CharacterPos ("Character Position", Vector) = (0, 0, 0, 0)
        _CharacterForward ("Character Forward", Vector) = (0, 0, 1, 0)
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 100
        
        Pass
        {
            Name "KillZonePass"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _EdgeColor;
                float _Range;
                float _ForwardFalloff;
                float _EdgeWidth;
                float _PulseSpeed;
                float _PulseIntensity;
                float _GridScale;
                float _GridIntensity;
                float4 _CharacterPos;
                float4 _CharacterForward;
            CBUFFER_END
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                // Calculate direction from character to this pixel (on ground plane)
                float3 toPixel = IN.positionWS - _CharacterPos.xyz;
                toPixel.y = 0; // Flatten to ground plane
                
                float dist = length(toPixel);
                
                // Simple distance-based visibility first
                float distanceFactor = 1.0 - saturate(dist / _Range);
                
                // If outside range, don't render
                if (dist > _Range)
                    return half4(0, 0, 0, 0);
                
                float3 dirToPixel = normalize(toPixel + 0.0001);
                
                // Calculate angle from forward direction
                float3 forward = normalize(float3(_CharacterForward.x, 0, _CharacterForward.z) + 0.0001);
                float dotProduct = dot(forward, dirToPixel);
                
                // Forward factor: 1 in front, 0 at sides/back
                float forwardFactor = saturate(dotProduct);
                forwardFactor = pow(forwardFactor, _ForwardFalloff);
                
                // Pulse animation
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseIntensity + 1.0;
                
                // Edge glow
                float edgeDist = abs(dist - _Range * 0.9);
                float edgeFactor = 1.0 - saturate(edgeDist / (_Range * _EdgeWidth));
                edgeFactor *= forwardFactor;
                
                // Final alpha
                float alpha = distanceFactor * forwardFactor * pulse * _Color.a;
                alpha = max(alpha, edgeFactor * _EdgeColor.a * 0.5);
                
                // Color
                half4 finalColor = lerp(_Color, _EdgeColor, edgeFactor);
                finalColor.a = alpha;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    // Fallback for non-URP or if URP pass fails
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 2.0
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };
            
            float4 _Color;
            float4 _EdgeColor;
            float _Range;
            float _ForwardFalloff;
            float4 _CharacterPos;
            float4 _CharacterForward;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float3 toPixel = i.worldPos - _CharacterPos.xyz;
                toPixel.y = 0;
                
                float dist = length(toPixel);
                if (dist > _Range)
                    return fixed4(0, 0, 0, 0);
                
                float distanceFactor = 1.0 - saturate(dist / _Range);
                
                float3 dirToPixel = normalize(toPixel + 0.0001);
                float3 forward = normalize(float3(_CharacterForward.x, 0, _CharacterForward.z) + 0.0001);
                float dotProduct = dot(forward, dirToPixel);
                
                float forwardFactor = saturate(dotProduct);
                forwardFactor = pow(forwardFactor, _ForwardFalloff);
                
                float alpha = distanceFactor * forwardFactor * _Color.a;
                
                fixed4 finalColor = _Color;
                finalColor.a = alpha;
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
