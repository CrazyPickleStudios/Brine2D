namespace Brine2D.Rendering.SDL.PostProcessing.Shaders;

/// <summary>
/// HLSL shader source for grayscale post-processing effect.
/// Cross-platform via ShaderCross (HLSL → SPIRV/DXIL/MSL).
/// </summary>
internal static class GrayscaleShader
{
    // Vertex shader in HLSL (cross-platform via ShaderCross)
    public const string VertexShaderSource = @"
struct VSOutput
{
    float4 position : SV_Position;
    float2 texCoord : TEXCOORD0;
};

VSOutput main(uint vertexID : SV_VertexID)
{
    VSOutput output;
    
    // Full-screen triangle trick
    // vertex 0: (-1, -1) -> uv (0, 0)
    // vertex 1: (3, -1)  -> uv (2, 0)
    // vertex 2: (-1, 3)  -> uv (0, 2)
    float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
    output.texCoord = uv;
    output.position = float4(uv * float2(2.0, 2.0) - float2(1.0, 1.0), 0.0, 1.0);
    
    return output;
}";

    // Fragment shader in HLSL
    // NOTE: For post-processing, texture is bound to set 1, binding 0 in SDL3 GPU
    public const string FragmentShaderSource = @"
struct PSInput
{
    float4 position : SV_Position;
    float2 texCoord : TEXCOORD0;
};

// Texture and sampler - binding might need adjustment based on SDL3 GPU API
Texture2D inputTexture : register(t0, space1);
SamplerState inputSampler : register(s0, space1);

float4 main(PSInput input) : SV_Target
{
    float4 color = inputTexture.Sample(inputSampler, input.texCoord);
    
    // Calculate luminance using standard weights
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
    
    return float4(gray, gray, gray, color.a);
}";
}