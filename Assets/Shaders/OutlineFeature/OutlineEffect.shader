Shader "Hidden/OutlineEffect"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Float) = 2
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        
        float4 _OutlineColor;
        float _OutlineWidth;
        
        TEXTURE2D(_MaskTex);
        SAMPLER(sampler_MaskTex);
        
        float4 _BlitTexture_TexelSize;
        ENDHLSL
        
        // Pass 0: Just copy (placeholder for mask creation)
        Pass
        {
            Name "Copy"
            ZTest Always
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            
            half4 frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
            }
            ENDHLSL
        }
        
        // Pass 1: Edge detection and outline
        Pass
        {
            Name "Outline"
            ZTest Always
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            
            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 texelSize = _BlitTexture_TexelSize.xy * _OutlineWidth;
                
                half4 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                
                // Sample depth in a cross pattern for edge detection
                float d0 = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r, _ZBufferParams);
                float d1 = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(texelSize.x, 0)).r, _ZBufferParams);
                float d2 = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(-texelSize.x, 0)).r, _ZBufferParams);
                float d3 = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(0, texelSize.y)).r, _ZBufferParams);
                float d4 = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(0, -texelSize.y)).r, _ZBufferParams);
                
                // Sobel-like edge detection
                float edge = abs(d1 - d2) + abs(d3 - d4);
                edge = saturate(edge * 100); // Amplify edges
                
                // Blend outline color
                half4 result = lerp(original, _OutlineColor, edge * _OutlineColor.a);
                return result;
            }
            ENDHLSL
        }
    }
}
