// Grayscale post-processing fragment shader
// Converts RGB to grayscale using luminance formula

// NOTE: Use space2 to match regular rendering bindings
Texture2D inputTexture : register(t0, space2);
SamplerState inputSampler : register(s0, space2);

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
    
    return float4(gray, gray, gray, color.a);
}