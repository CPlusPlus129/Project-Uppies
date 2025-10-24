Shader "Billboard/Sprite Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Sprite Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        
        [Header(Billboard Settings)]
        _BillboardType("Billboard Type", Float) = 0  // 0 = Full, 1 = Y-Axis only
        
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            
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
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float4 tangentWS    : TEXCOORD3;
                float fogFactor     : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BillboardType;
                half _Cutoff;
                half _BumpScale;
            CBUFFER_END

            Varyings BillboardVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

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

                // Billboard normals (always facing camera for proper lighting)
                output.normalWS = -forward;  // Face camera
                
                // Tangent for normal mapping
                output.tangentWS = float4(right, 1.0);

                // Fog
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 BillboardFragment(Varyings input) : SV_Target
            {
                // Sample base texture
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color = baseMap * _BaseColor;

                // Alpha cutout
                clip(color.a - _Cutoff);

                // Prepare lighting input
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                // Normal mapping (optional)
                #ifdef _NORMALMAP
                    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                    half3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                    half3x3 TBN = half3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                    lightingInput.normalWS = normalize(mul(normalTS, TBN));
                #else
                    lightingInput.normalWS = normalize(input.normalWS);
                #endif

                // Simple lit shading
                Light mainLight = GetMainLight(lightingInput.shadowCoord);
                
                half3 ambient = SampleSH(lightingInput.normalWS);
                half3 diffuse = mainLight.color * mainLight.distanceAttenuation * 
                                saturate(dot(lightingInput.normalWS, mainLight.direction));

                // Additional lights
                #ifdef _ADDITIONAL_LIGHTS
                    int additionalLightsCount = GetAdditionalLightsCount();
                    for (int i = 0; i < additionalLightsCount; ++i)
                    {
                        Light light = GetAdditionalLight(i, input.positionWS);
                        diffuse += light.color * light.distanceAttenuation * 
                                   saturate(dot(lightingInput.normalWS, light.direction));
                    }
                #endif

                // Combine lighting
                half3 lighting = ambient + diffuse;
                color.rgb *= lighting;

                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogFactor);

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
                half _Cutoff;
                half _BumpScale;
            CBUFFER_END

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                
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
                half _Cutoff;
                half _BumpScale;
            CBUFFER_END

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                
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
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
