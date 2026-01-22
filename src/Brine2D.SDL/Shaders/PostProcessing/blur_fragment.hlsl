// Blur post-processing fragment shader
// Uses SDL3 GPU uniform buffer convention (space3 for fragment uniforms)

Texture2D inputTexture : register(t0, space2);
SamplerState inputSampler : register(s0, space2);

// Fragment uniform buffer - MUST be space3 for SDL3 GPU!
struct BlurUniforms
{
    float2 direction;      // (1,0) for horizontal, (0,1) for vertical
    float blurRadius;      // Blur strength
    float _padding;        // 16-byte alignment
};

[[vk::binding(0, 3)]]  // Binding 0, Set 3 (space3 for fragment uniforms)
ConstantBuffer<BlurUniforms> uniforms : register(b0, space3);

struct PSInput
{
    float4 position : SV_Position;
    float2 texCoord : TEXCOORD0;
};

float4 main(PSInput input) : SV_Target
{
    float2 texelSize;
    inputTexture.GetDimensions(texelSize.x, texelSize.y);
    texelSize = 1.0 / texelSize;
    
    // Calculate blur offset using uniforms
    float2 offset = uniforms.direction * texelSize * uniforms.blurRadius;
    
    // 5-tap Gaussian blur
    float4 color = float4(0, 0, 0, 0);
    color += inputTexture.Sample(inputSampler, input.texCoord - offset * 2.0) * 0.0545;
    color += inputTexture.Sample(inputSampler, input.texCoord - offset * 1.0) * 0.2442;
    color += inputTexture.Sample(inputSampler, input.texCoord) * 0.4026;
    color += inputTexture.Sample(inputSampler, input.texCoord + offset * 1.0) * 0.2442;
    color += inputTexture.Sample(inputSampler, input.texCoord + offset * 2.0) * 0.0545;
    
    return color;
}