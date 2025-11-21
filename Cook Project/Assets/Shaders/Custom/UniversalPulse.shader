Shader "Custom/UniversalPulse"
{
    Properties
    {
        _Color ("Glow Color", Color) = (0.0, 1.0, 1.0, 1)
        _Intensity ("Glow Intensity", Range(0, 10)) = 2
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3
        
        _Expansion ("Expansion Amount", Range(0, 1)) = 0
        _MaxOutline ("Max Outline Distance", Range(0, 2)) = 0.2
        _Opacity ("Opacity", Range(0, 1)) = 1.0
        
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 // Default to LessEqual
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
            Name "UniversalPulsePass"
            Tags { "LightMode" = "UniversalForward" }
            
            // Render settings
            ZWrite Off
            ZTest [_ZTest]
            Cull Off      
            Blend SrcAlpha One  // Additive
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _FresnelPower;
                float _Expansion;
                float _MaxOutline;
                float _Opacity;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // Calculate expansion in Object Space
                // Expand along normal with a tiny base inflation to prevent Z-fighting at 0
                float baseInflation = 0.002;
                float3 expandedPosOS = input.positionOS.xyz + (input.normalOS * (baseInflation + _Expansion * _MaxOutline));
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(expandedPosOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Fresnel
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                
                // Combine
                float glowStrength = fresnel * _Intensity * _Opacity;
                half4 finalColor = _Color * glowStrength;
                
                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
