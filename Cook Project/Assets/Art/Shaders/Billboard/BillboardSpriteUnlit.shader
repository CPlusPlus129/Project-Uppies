Shader "Billboard/Sprite Unlit"
{
    Properties
    {
        [MainTexture] _BaseMap("Sprite Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        
        [Header(Billboard Settings)]
        _BillboardType("Billboard Type", Float) = 0  // 0 = Full, 1 = Y-Axis only
        
        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_fog
            
            #pragma vertex BillboardVertex
            #pragma fragment BillboardFragment
            
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
                float fogFactor     : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BillboardType;
                half _Cutoff;
            CBUFFER_END

            Varyings BillboardVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

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
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 BillboardFragment(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(color.a - _Cutoff);
                
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }

        // Shadow caster (same as lit version)
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
            CBUFFER_END

            Varyings ShadowPassVertex(Attributes input)
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

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
