// Grayscale post-processing fragment shader
// Converts RGB to grayscale using luminance formula
// Intensity uniform allows blending between original and grayscale

// NOTE: Use space2 to match regular rendering bindings
Texture2D inputTexture : register(t0, space2);
SamplerState inputSampler : register(s0, space2);

// Uniform buffer matching GrayscaleParams (space3 for fragment uniforms)
cbuffer GrayscaleParams : register(b0, space3)
{
    float intensity;
    float _padding1;
    float _padding2;
    float _padding3;
};

struct PSInput
{
    float4 position : SV_Position;
    float2 texCoord : TEXCOORD0;
};

float4 main(PSInput input) : SV_Target
{
    float4 color = inputTexture.Sample(inputSampler, input.texCoord);

    // Calculate luminance using standard weights
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));

    // Blend between original color and grayscale based on intensity
    float3 result = lerp(color.rgb, float3(gray, gray, gray), intensity);

    return float4(result, color.a);
}