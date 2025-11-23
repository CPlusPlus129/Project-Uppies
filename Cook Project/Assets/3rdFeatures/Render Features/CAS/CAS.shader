Shader "CustomEffects/CAS"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    float _Sharpness;

    // Helper to sample texture with offset
    float3 SampleOffset(float2 uv, float2 offset)
    {
        return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset).rgb;
    }

    float4 CASFragment(Varyings input) : SV_Target
    {
        float2 uv = input.texcoord;
        float2 texelSize = _BlitTexture_TexelSize.xy;
        float px = texelSize.x;
        float py = texelSize.y;

        // Fetch 3x3 neighborhood
        // [ 1 2 3 ]
        // [ 4 o 6 ]
        // [ 7 8 9 ]
        float3 c2 = SampleOffset(uv, float2(0, -py));
        float3 c4 = SampleOffset(uv, float2(-px, 0));
        float3 c6 = SampleOffset(uv, float2(px, 0));
        float3 c8 = SampleOffset(uv, float2(0, py));
        float3 ori = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb;

        // Diagonal
        float3 c1 = SampleOffset(uv, float2(-px, -py));
        float3 c3 = SampleOffset(uv, float2(px, -py));
        float3 c7 = SampleOffset(uv, float2(-px, py));
        float3 c9 = SampleOffset(uv, float2(px, py));

        // Min/Max for local contrast
        float3 minRGB = min(min(min(c2, c4), min(c6, c8)), ori);
        float3 maxRGB = max(max(max(c2, c4), max(c6, c8)), ori);

        minRGB += min(minRGB, min(min(min(c1, c3), min(c7, c9)), minRGB));
        maxRGB += max(maxRGB, max(max(max(c1, c3), max(c7, c9)), maxRGB));

        // Soft min/max
        float3 ampRGB = saturate(min(minRGB, 2.0 - maxRGB) / maxRGB);
        
        // Shaping amount
        float peak = -3.0 * _Sharpness + 8.0; 
        float3 wRGB = -1.0 / (ampRGB * peak); 

        float3 rcpWeight = 1.0 / (4.0 * wRGB + 1.0);

        // Sharpening
        float3 sharp = saturate((c2 * wRGB + c4 * wRGB + c6 * wRGB + c8 * wRGB + ori) * rcpWeight);

        return float4(sharp, 1.0);
    }
    
    // Simplified version closer to the gist if the above is too complex or different
    // The gist logic:
    // #define peak (3*CAS -8.)
    // wRGB = sqrt(ampRGB) * 1.0 / peak; 
    // sharp = ori + (c2+c4+c6+c8)*wRGB;
    // sharp = saturate(sharp * rcp(4*wRGB + 1.));
    
    // Let's stick closer to the gist math for correctness:
    float4 CASFragmentGist(Varyings input) : SV_Target
    {
        float2 uv = input.texcoord;
        float2 texelSize = _BlitTexture_TexelSize.xy;
        float px = texelSize.x;
        float py = texelSize.y;
        
        float3 c2 = SampleOffset(uv, float2(0, -py));
        float3 c4 = SampleOffset(uv, float2(-px, 0));
        float3 c6 = SampleOffset(uv, float2(px, 0));
        float3 c8 = SampleOffset(uv, float2(0, py));
        float3 ori = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb;
        
        float3 minRGB = min(min(min(c2, c4), min(c6, c8)), ori);
        float3 maxRGB = max(max(max(c2, c4), max(c6, c8)), ori);
        
        float3 sumCross = c2 + c4 + c6 + c8;
        
        float3 c1 = SampleOffset(uv, float2(-px, -py));
        float3 c3 = SampleOffset(uv, float2(px, -py));
        float3 c7 = SampleOffset(uv, float2(-px, py));
        float3 c9 = SampleOffset(uv, float2(px, py));
        
        minRGB += min(minRGB, min(min(min(c1, c3), min(c7, c9)), minRGB));
        maxRGB += max(maxRGB, max(max(max(c1, c3), max(c7, c9)), maxRGB));
        
        minRGB = min(minRGB, 2.0 - maxRGB);
        float3 ampRGB = minRGB * rcp(maxRGB + 0.00001); // avoid div by zero
        
        // _Sharpness as CAS parameter
        float peak = 3.0 * _Sharpness - 8.0;
        float3 wRGB = sqrt(ampRGB) * (1.0 / peak);
        
        float3 sharp = ori + sumCross * wRGB;
        sharp = saturate(sharp * rcp(4.0 * wRGB + 1.0));
        
        return float4(sharp, 1.0);
    }
    
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off

        Pass
        {
            Name "CASPass"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment CASFragmentGist
            ENDHLSL
        }
    }
}
