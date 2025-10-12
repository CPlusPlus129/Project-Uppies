Shader "Custom/LightIndicatorTranslucent"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0.3)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 1.0
        _BlendMode("Blend Mode", Float) = 20
        
        // Blend state properties (set by script)
        [HideInInspector] _SrcBlend("Src Blend", Float) = 1
        [HideInInspector] _DstBlend("Dst Blend", Float) = 10
        
        // Metaball-specific properties
        _DepthFade ("Depth Fade Distance", Range(0, 5)) = 1.0
        _CenterFalloff ("Center Falloff", Range(0.1, 2.0)) = 0.8
        _BlobThreshold ("Blob Merge Threshold", Range(0, 1)) = 0.5
        _MaxBrightness ("Max Brightness", Range(0.1, 2.0)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }
        
        Pass
        {
            Name "LightIndicator"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            
            // Dynamic blending controlled by material properties
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float fogCoord : TEXCOORD4;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _RimPower;
                float _RimIntensity;
                float _BlendMode;
                float _DepthFade;
                float _CenterFalloff;
                float _BlobThreshold;
                float _MaxBrightness;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.positionWS = vertexInput.positionWS;
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Fresnel/Rim effect - makes edges glow
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, _RimPower) * _RimIntensity;
                
                half4 color = _Color;
                
                // Branch based on blend mode
                if (_BlendMode < 5.0) 
                {
                    // ===== ADDITIVE MODE (BlendMode = 1) =====
                    // Simple additive blending with fresnel rim
                    color.a *= fresnel;
                }
                else if (_BlendMode < 15.0) 
                {
                    // ===== ALPHA BLEND MODE (BlendMode = 10) =====
                    // Standard alpha blending with enhanced rim
                    color.rgb += fresnel * _Color.rgb;
                }
                else 
                {
                    // ===== METABALL MODE (BlendMode = 20) =====
                    // Advanced metaball blending with brightness ceiling
                    
                    // Calculate screen UVs for depth sampling
                    float2 screenUV = input.screenPos.xy / input.screenPos.w;
                    
                    // Sample scene depth
                    #if UNITY_REVERSED_Z
                        float sceneDepth = SampleSceneDepth(screenUV);
                    #else
                        float sceneDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                    #endif
                    
                    // Convert to linear depth
                    float sceneDepthLinear = LinearEyeDepth(sceneDepth, _ZBufferParams);
                    float surfaceDepthLinear = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                    
                    // Calculate depth fade for soft particles
                    float depthDifference = sceneDepthLinear - surfaceDepthLinear;
                    float depthFade = saturate(depthDifference / max(_DepthFade, 0.001));
                    
                    // Center-weighted falloff - stronger in center, weaker at edges
                    float viewDot = saturate(dot(normalWS, viewDirWS));
                    float centerWeight = pow(viewDot, _CenterFalloff);
                    
                    // Inverse fresnel for metaball density - more solid in center
                    float metaballDensity = 1.0 - pow(1.0 - viewDot, _RimPower * 0.5);
                    
                    // Combine density factors
                    float finalDensity = metaballDensity * centerWeight * depthFade;
                    
                    // Apply blob threshold - creates cleaner merging
                    finalDensity = smoothstep(_BlobThreshold, 1.0, finalDensity);
                    
                    // Scale RGB by density and rim intensity
                    color.rgb *= finalDensity * _RimIntensity;
                    
                    // Clamp RGB to max brightness per sphere
                    color.rgb = min(color.rgb, _MaxBrightness);
                    
                    // CRITICAL: Calculate brightness-proportional alpha
                    // This creates the brightness ceiling when using Blend One OneMinusSrcAlpha
                    // As output brightness approaches _MaxBrightness, alpha increases
                    // Higher alpha = stronger blocking of background layers
                    float maxComponent = max(color.r, max(color.g, color.b));
                    float brightnessRatio = saturate(maxComponent / max(_MaxBrightness, 0.001));
                    
                    // Alpha formula: increases with both density and brightness
                    // The 0.5 + 0.5 * brightnessRatio creates a range of [0.5, 1.0]
                    // This ensures alpha increases as we approach max brightness
                    color.a = saturate(finalDensity * _Color.a * (0.5 + 0.5 * brightnessRatio));
                }
                
                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogCoord);
                
                return color;
            }
            ENDHLSL
        }
    }
    
    Fallback Off
}
