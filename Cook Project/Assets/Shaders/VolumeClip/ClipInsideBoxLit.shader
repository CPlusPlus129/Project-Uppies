Shader "Uppies/ClipInsideBox/Lit"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Color Tint", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        [PerRendererData]_ClipBoxEnabled ("Clip Enabled", Float) = 0
        [PerRendererData]_ClipBoxFeather ("Clip Feather", Float) = 0.05
        [PerRendererData]_ClipBoxCenter ("Clip Center", Vector) = (0,0,0,0)
        [PerRendererData]_ClipBoxExtents ("Clip Extents", Vector) = (0.5,0.5,0.5,0)
        [PerRendererData]_ClipBoxWorldToLocal_Col0 ("Clip WorldToLocal Col0", Vector) = (1,0,0,0)
        [PerRendererData]_ClipBoxWorldToLocal_Col1 ("Clip WorldToLocal Col1", Vector) = (0,1,0,0)
        [PerRendererData]_ClipBoxWorldToLocal_Col2 ("Clip WorldToLocal Col2", Vector) = (0,0,1,0)
        [PerRendererData]_ClipBoxWorldToLocal_Col3 ("Clip WorldToLocal Col3", Vector) = (0,0,0,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 texcoord   : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float3 viewDirWS  : TEXCOORD3;
                float fogCoord    : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Smoothness;
            CBUFFER_END

            float _ClipBoxEnabled;
            float _ClipBoxFeather;
            float4 _ClipBoxCenter;
            float4 _ClipBoxExtents;
            float4 _ClipBoxWorldToLocal_Col0;
            float4 _ClipBoxWorldToLocal_Col1;
            float4 _ClipBoxWorldToLocal_Col2;
            float4 _ClipBoxWorldToLocal_Col3;

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                output.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS);

                return output;
            }

            float ComputeClipFactor(float3 positionWS)
            {
                float clipEnabled = _ClipBoxEnabled;
                float clipFeather = _ClipBoxFeather;
                float3 clipCenter = _ClipBoxCenter.xyz;
                float3 clipExtents = _ClipBoxExtents.xyz;
                float4x4 clipWorldToLocal = float4x4(
                    _ClipBoxWorldToLocal_Col0,
                    _ClipBoxWorldToLocal_Col1,
                    _ClipBoxWorldToLocal_Col2,
                    _ClipBoxWorldToLocal_Col3);

                if (clipEnabled < 0.5)
                {
                    return 1.0;
                }

                float3 positionLS = mul(clipWorldToLocal, float4(positionWS, 1.0)).xyz - clipCenter;
                float3 delta = abs(positionLS) - clipExtents;
                float clipDistance = max(delta.x, max(delta.y, delta.z));

                if (clipFeather <= 1e-5f)
                {
                    clip(clipDistance);
                    return 1.0;
                }

                float feather = max(clipFeather, 1e-5f);
                float fade = saturate((clipDistance + feather) / feather);
                clip(fade - 1e-3f);
                return fade;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

#if defined(LIGHTMAP_ON)
                normalWS = NormalizeNormalPerPixel(normalWS);
#endif

                float fade = ComputeClipFactor(input.positionWS);

                float4 albedoSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float3 albedo = albedoSample.rgb * _BaseColor.rgb;
                float alpha = albedoSample.a * _BaseColor.a;

                Light mainLight = GetMainLight(input.shadowCoord);
                float3 lighting = albedo * mainLight.color * saturate(dot(normalWS, mainLight.direction)) * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

#ifdef _ADDITIONAL_LIGHTS
                uint additionalLightsCount = GetAdditionalLightsCount();
                for (uint i = 0; i < additionalLightsCount; i++)
                {
                    Light light = GetAdditionalLight(i, input.positionWS);
                    float ndotl = saturate(dot(normalWS, light.direction));
                    lighting += albedo * light.color * ndotl * light.distanceAttenuation * light.shadowAttenuation;
                }
#endif

                float3 ambient = SampleSH(normalWS) * albedo;
                float3 color = (lighting + ambient) * fade;

                color = MixFog(color, input.fogCoord);

                return half4(color, alpha * fade);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
