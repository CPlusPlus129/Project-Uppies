Shader "Billboard/Sprite Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Sprite Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        
        [Header(Billboard Settings)]
        _BillboardType("Billboard Type", Float) = 0  // 0 = Full, 1 = Y-Axis only
        
        [Header(Lighting Mode)]
        [Enum(Billboard Normal,0,World Up,1,Spherical,2)] _LightingMode("Lighting Normal", Float) = 1
        
        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 0  // Off for double-sided
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Header(Lighting)]
        [Toggle(_NORMALMAP)] _UseNormalMap("Use Normal Map", Float) = 0
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "DisableBatching" = "True"  // Required for billboard effect
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma target 4.5
            
            // Unity URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            
            // Unity GI keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            
            #pragma shader_feature_local _NORMALMAP
            
            #pragma vertex BillboardVertex
            #pragma fragment BillboardFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS               : SV_POSITION;
                float2 uv                       : TEXCOORD0;
                float3 positionWS               : TEXCOORD1;
                float3 normalWS                 : TEXCOORD2;
                float4 tangentWS                : TEXCOORD3;
                half4 fogFactorAndVertexLight   : TEXCOORD4;
                
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord          : TEXCOORD5;
                #endif
                
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
                #ifdef DYNAMICLIGHTMAP_ON
                    float2 dynamicLightmapUV    : TEXCOORD7;
                #endif
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BillboardType;
                half _LightingMode;
                half _Cutoff;
                half _BumpScale;
            CBUFFER_END

            Varyings BillboardVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Get object position in world space
                float3 objectPosWS = TransformObjectToWorld(float3(0, 0, 0));
                
                // Calculate billboard rotation based on camera
                float3 forward = normalize(_WorldSpaceCameraPos - objectPosWS);
                
                // Billboard type: 0 = full billboard, 1 = Y-axis only
                if (_BillboardType > 0.5)
                {
                    // Y-axis billboard (for trees, characters standing upright)
                    forward.y = 0;
                    forward = normalize(forward);
                }
                
                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, forward));
                up = cross(forward, right);

                // Transform vertex using billboard basis
                float3 vertexWS = objectPosWS + 
                    right * input.positionOS.x + 
                    up * input.positionOS.y;

                output.positionWS = vertexWS;
                output.positionCS = TransformWorldToHClip(vertexWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                // Calculate lighting normal based on mode
                half3 lightingNormal;
                if (_LightingMode < 0.5)
                {
                    // Billboard Normal mode - faces camera
                    lightingNormal = forward;
                }
                else if (_LightingMode < 1.5)
                {
                    // World Up mode - best for overhead lights
                    lightingNormal = half3(0, 1, 0);
                }
                else
                {
                    // Spherical mode - blend for rounded appearance
                    half3 toCamera = normalize(_WorldSpaceCameraPos - vertexWS);
                    lightingNormal = normalize(lerp(half3(0, 1, 0), toCamera, 0.5));
                }
                
                output.normalWS = lightingNormal;
                
                // Tangent for normal mapping
                output.tangentWS = float4(right, 1.0);

                // Calculate vertex lighting and fog
                half3 vertexLight = VertexLighting(vertexWS, lightingNormal);
                half fogFactor = ComputeFogFactor(output.positionCS.z);
                output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

                // Shadow coordinates
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = TransformWorldToShadowCoord(vertexWS);
                #endif
                
                // Lightmap UVs
                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                #ifdef DYNAMICLIGHTMAP_ON
                    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                OUTPUT_SH(output.normalWS, output.vertexSH);

                return output;
            }

            // Initialize InputData for URP lighting
            void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
            {
                inputData = (InputData)0;

                inputData.positionWS = input.positionWS;
                
                // Handle normal mapping if enabled
                #ifdef _NORMALMAP
                    half3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                    inputData.normalWS = normalize(mul(normalTS, tangentToWorld));
                #else
                    inputData.normalWS = normalize(input.normalWS);
                #endif

                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                
                // Shadow coordinates
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    inputData.shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                // Fog
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                
                // Vertex lighting
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                
                // Baked GI - THIS IS THE CRITICAL PART FOR LIGHTING
                #if defined(DYNAMICLIGHTMAP_ON)
                    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
                #else
                    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                #endif
                
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
                
                #if defined(DEBUG_DISPLAY)
                    #if defined(DYNAMICLIGHTMAP_ON)
                        inputData.dynamicLightmapUV = input.dynamicLightmapUV;
                    #endif
                    #if defined(LIGHTMAP_ON)
                        inputData.staticLightmapUV = input.staticLightmapUV;
                    #else
                        inputData.vertexSH = input.vertexSH;
                    #endif
                #endif
            }

            half4 BillboardFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Sample base texture
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 albedoAlpha = baseMap * _BaseColor;

                // Alpha cutout
                clip(albedoAlpha.a - _Cutoff);

                // Sample normal map if enabled
                half3 normalTS = half3(0, 0, 1);
                #ifdef _NORMALMAP
                    normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                #endif

                // Initialize InputData
                InputData inputData;
                InitializeInputData(input, normalTS, inputData);

                // Initialize SurfaceData
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedoAlpha.rgb;
                surfaceData.alpha = albedoAlpha.a;
                surfaceData.metallic = 0.0;
                surfaceData.specular = half3(0.0, 0.0, 0.0);
                surfaceData.smoothness = 0.0;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = half3(0, 0, 0);
                surfaceData.occlusion = 1.0;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                // Use Unity's built-in Blinn-Phong lighting (perfect for sprites)
                half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);

                // Final output
                color.a = albedoAlpha.a;
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
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BillboardType;
                half _LightingMode;
                half _Cutoff;
                half _BumpScale;
            CBUFFER_END

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                
                // Get object position in world space
                float3 objectPosWS = TransformObjectToWorld(float3(0, 0, 0));
                
                // Calculate billboard rotation
                float3 forward = normalize(_WorldSpaceCameraPos - objectPosWS);
                
                if (_BillboardType > 0.5)
                {
                    forward.y = 0;
                    forward = normalize(forward);
                }
                
                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, forward));
                up = cross(forward, right);

                // Transform vertex
                float3 vertexWS = objectPosWS + 
                    right * input.positionOS.x + 
                    up * input.positionOS.y;

                output.positionCS = TransformWorldToHClip(vertexWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
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
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BillboardType;
                half _LightingMode;
                half _Cutoff;
                half _BumpScale;
            CBUFFER_END

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                
                float3 objectPosWS = TransformObjectToWorld(float3(0, 0, 0));
                float3 forward = normalize(_WorldSpaceCameraPos - objectPosWS);
                
                if (_BillboardType > 0.5)
                {
                    forward.y = 0;
                    forward = normalize(forward);
                }
                
                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, forward));
                up = cross(forward, right);

                float3 vertexWS = objectPosWS + 
                    right * input.positionOS.x + 
                    up * input.positionOS.y;

                output.positionCS = TransformWorldToHClip(vertexWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half DepthOnlyFragment(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return input.positionCS.z;
            }
            ENDHLSL
        }
        
        // DepthNormals pass for SSAO
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float2 uv           : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BillboardType;
                half _LightingMode;
                half _Cutoff;
                half _BumpScale;
            CBUFFER_END

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                
                float3 objectPosWS = TransformObjectToWorld(float3(0, 0, 0));
                float3 forward = normalize(_WorldSpaceCameraPos - objectPosWS);
                
                if (_BillboardType > 0.5)
                {
                    forward.y = 0;
                    forward = normalize(forward);
                }
                
                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, forward));
                up = cross(forward, right);

                float3 vertexWS = objectPosWS + 
                    right * input.positionOS.x + 
                    up * input.positionOS.y;

                output.positionCS = TransformWorldToHClip(vertexWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                // Use lighting normal
                half3 lightingNormal;
                if (_LightingMode < 0.5)
                {
                    lightingNormal = forward;
                }
                else if (_LightingMode < 1.5)
                {
                    lightingNormal = half3(0, 1, 0);
                }
                else
                {
                    half3 toCamera = normalize(_WorldSpaceCameraPos - vertexWS);
                    lightingNormal = normalize(lerp(half3(0, 1, 0), toCamera, 0.5));
                }
                
                output.normalWS = lightingNormal;

                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                
                return half4(normalize(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
