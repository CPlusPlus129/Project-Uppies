Shader "Custom/LightRevealTexture"
{
    Properties
    {
        [Header(Textures)]
        [MainTexture] _BaseMap("Base Texture (Dark)", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color Tint", Color) = (1,1,1,1)
        _RevealMap("Reveal Texture (Light)", 2D) = "white" {}
        _RevealColor("Reveal Color Tint", Color) = (1,1,1,1)
        
        [Header(Normal Maps)]
        _BaseNormalMap("Base Normal Map", 2D) = "bump" {}
        _BaseNormalStrength("Base Normal Strength", Range(0, 2)) = 1.0
        _RevealNormalMap("Reveal Normal Map", 2D) = "bump" {}
        _RevealNormalStrength("Reveal Normal Strength", Range(0, 2)) = 1.0
        
        [Header(Light Reveal Settings)]
        _RevealSensitivity("Reveal Sensitivity", Range(0.1, 10.0)) = 1.0
        _RevealSmoothness("Reveal Smoothness", Range(0.01, 1.0)) = 0.2
        _MaxRevealBrightness("Max Reveal Brightness", Range(0.1, 5.0)) = 2.0
        _MinimumLight("Minimum Light Threshold", Range(0.0, 1.0)) = 0.1
        
        [Header(Surface Properties)]
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0.0
        
        [Header(Advanced)]
        _LightDistanceFalloff("Light Distance Falloff", Range(0.1, 5.0)) = 1.0
        [Toggle(USE_REVEAL_EMISSIVE)] _UseRevealEmissive("Reveal Texture Emissive", Float) = 0
        _RevealEmissiveStrength("Reveal Emissive Strength", Range(0, 5)) = 1.0
        
        [Header(Debug)]
        [Toggle(DEBUG_LIGHT_ACCUMULATION)] _DebugLightAccumulation("Debug: Show Light Accumulation", Float) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        LOD 300
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma target 4.5
            
            // Unity 6.2 URP required pragmas
            #pragma vertex LightRevealVertex
            #pragma fragment LightRevealFragment
            
            // URP lighting features
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS
            
            // Unity standard features
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            // Shader features
            #pragma shader_feature_local USE_REVEAL_EMISSIVE
            #pragma shader_feature_local DEBUG_LIGHT_ACCUMULATION
            
            // URP shader includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            
            // Material properties
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_RevealMap);
            SAMPLER(sampler_RevealMap);
            TEXTURE2D(_BaseNormalMap);
            SAMPLER(sampler_BaseNormalMap);
            TEXTURE2D(_RevealNormalMap);
            SAMPLER(sampler_RevealNormalMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _RevealMap_ST;
                float4 _RevealColor;
                float _BaseNormalStrength;
                float _RevealNormalStrength;
                float _RevealSensitivity;
                float _RevealSmoothness;
                float _MaxRevealBrightness;
                float _MinimumLight;
                float _Smoothness;
                float _Metallic;
                float _LightDistanceFalloff;
                float _RevealEmissiveStrength;
            CBUFFER_END
            
            // Vertex input
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            // Vertex output / Fragment input
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 baseUV : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float fogFactor : TEXCOORD5;
                float2 revealUV : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            // Vertex shader
            Varyings LightRevealVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // Transform positions
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                // Transform normals
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                
                // UVs
                float2 originalUV = input.texcoord;
                output.baseUV = TRANSFORM_TEX(originalUV, _BaseMap);
                output.revealUV = TRANSFORM_TEX(originalUV, _RevealMap);
                
                // Fog
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            // Helper function: Unpack and blend normal maps
            float3 BlendNormals(float3 baseNormal, float3 revealNormal, float revealAmount)
            {
                // Blend normals using whiteout blending (best for detail normals)
                float3 blended = float3(
                    baseNormal.xy + revealNormal.xy * revealAmount,
                    baseNormal.z * revealNormal.z
                );
                return normalize(blended);
            }

            void InitializeLightRevealInputData(Varyings input, float3 normalWS, float3 viewDirWS, float4 shadowCoord, out InputData inputData)
            {
                inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.vertexLighting = float3(0, 0, 0);
                inputData.shadowMask = half4(1, 1, 1, 1);
            }
            
            // Fragment shader
            half4 LightRevealFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Normalize interpolated vectors
                float3 normalWS = normalize(input.normalWS);
                float3 tangentWS = normalize(input.tangentWS);
                float3 bitangentWS = normalize(input.bitangentWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float2 baseUV = input.baseUV;
                float2 revealUV = input.revealUV;
                
                // Sample textures
                half4 baseTexture = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
                half4 revealTexture = SAMPLE_TEXTURE2D(_RevealMap, sampler_RevealMap, revealUV);
                
                // Sample normal maps
                float3 baseNormalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BaseNormalMap, sampler_BaseNormalMap, baseUV),
                    _BaseNormalStrength
                );
                float3 revealNormalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_RevealNormalMap, sampler_RevealNormalMap, revealUV),
                    _RevealNormalStrength
                );
                
                // ===== CALCULATE LIGHT ACCUMULATION =====
                float totalLightIntensity = 0.0;
                
                // Get shadow coordinate for main light
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                
                // Prepare lighting input data (updated later once reveal normal is computed)
                InputData inputData;
                InitializeLightRevealInputData(input, normalWS, viewDirWS, shadowCoord, inputData);
                inputData.positionCS = input.positionCS;
                
                // Main directional light contribution
                Light mainLight = GetMainLight(shadowCoord);
                float mainLightNdotL = saturate(dot(normalWS, mainLight.direction));
                
                // Calculate main light intensity using luminance
                float mainLightBrightness = dot(mainLight.color, float3(0.299, 0.587, 0.114));
                float mainLightContribution = mainLightBrightness * mainLight.distanceAttenuation * mainLight.shadowAttenuation * mainLightNdotL;
                totalLightIntensity += mainLightContribution;
                
                // Additional lights (Point Lights, Spot Lights - your light balls!)
                #ifdef _ADDITIONAL_LIGHTS
                    uint pixelLightCount = GetAdditionalLightsCount();
                    half4 shadowMask = CalculateShadowMask(inputData);
                    
                    #if defined(_FORWARD_PLUS)
                        AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                    #endif
                    
                    #if defined(_FORWARD_PLUS)
                        // Forward+ path - need InputData for the loop
                        // Forward+ directional lights
                        #if defined(URP_FP_DIRECTIONAL_LIGHTS_COUNT)
                            for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                            {
                                Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                                
                                float NdotL = saturate(dot(normalWS, light.direction));
                                float lightBrightness = dot(light.color, float3(0.299, 0.587, 0.114));
                                float lightContribution = lightBrightness * light.distanceAttenuation * NdotL * light.shadowAttenuation;
                                lightContribution = pow(max(lightContribution, 0.0), _LightDistanceFalloff);
                                totalLightIntensity += lightContribution;
                            }
                        #endif
                        
                        // Forward+ clustered lights
                        LIGHT_LOOP_BEGIN(pixelLightCount)
                            Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                            
                            float NdotL = saturate(dot(normalWS, light.direction));
                            float lightBrightness = dot(light.color, float3(0.299, 0.587, 0.114));
                            float lightContribution = lightBrightness * light.distanceAttenuation * NdotL * light.shadowAttenuation;
                            lightContribution = pow(max(lightContribution, 0.0), _LightDistanceFalloff);
                            totalLightIntensity += lightContribution;
                        LIGHT_LOOP_END
                    #else
                        // Standard Forward path
                        for (uint i = 0u; i < pixelLightCount; ++i)
                        {
                            Light light = GetAdditionalLight(i, input.positionWS, shadowMask);
                            
                            float NdotL = saturate(dot(normalWS, light.direction));
                            float lightBrightness = dot(light.color, float3(0.299, 0.587, 0.114));
                            float lightContribution = lightBrightness * light.distanceAttenuation * NdotL * light.shadowAttenuation;
                            lightContribution = pow(max(lightContribution, 0.0), _LightDistanceFalloff);
                            totalLightIntensity += lightContribution;
                        }
                    #endif
                #endif
                
                // Apply sensitivity
                float lightAccumulation = totalLightIntensity * _RevealSensitivity;
                
                // Normalize to 0-1 range based on max brightness
                lightAccumulation = saturate(lightAccumulation / _MaxRevealBrightness);
                
                // Apply minimum light threshold (removes ambient light if desired)
                float thresholdRange = max(1.0 - _MinimumLight, 0.001);
                lightAccumulation = saturate((lightAccumulation - _MinimumLight) / thresholdRange);
                
                // Apply smoothstep for smooth reveal transition
                float revealAmount = smoothstep(0.0, _RevealSmoothness, lightAccumulation);
                
                #ifdef DEBUG_LIGHT_ACCUMULATION
                    // Debug mode: show light accumulation as grayscale
                    return half4(revealAmount.xxx, 1.0);
                #endif
                
                // Blend textures based on reveal amount
                half4 albedo = lerp(baseTexture * _BaseColor, revealTexture * _RevealColor, revealAmount);
                
                // Blend normals
                float3 normalTS = BlendNormals(baseNormalTS, revealNormalTS, revealAmount);
                
                // Transform normal from tangent to world space
                float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, normalWS);
                float3 finalNormalWS = normalize(mul(normalTS, tangentToWorld));

                // Update lighting input with final normal details
                inputData.normalWS = finalNormalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(finalNormalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.positionCS = input.positionCS;
                inputData.tangentToWorld = half3x3(half3(tangentWS), half3(bitangentWS), half3(finalNormalWS));
                
                // Setup surface data for PBR lighting
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = 1.0;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = 1.0;
                
                // Add emissive if reveal texture should glow
                #ifdef USE_REVEAL_EMISSIVE
                    surfaceData.emission = revealTexture.rgb * _RevealColor.rgb * revealAmount * _RevealEmissiveStrength;
                #else
                    surfaceData.emission = half3(0, 0, 0);
                #endif
                
                // Calculate final lit color using URP's PBR lighting
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                
                // Apply fog
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                
                return color;
            }
            
            ENDHLSL
        }
        
        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            float3 _LightDirection;
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            
            ENDHLSL
        }
        
        // Depth only pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
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
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
