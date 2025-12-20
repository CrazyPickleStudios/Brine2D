namespace Brine2D.Rendering.SDL.Shaders;

/// <summary>
/// Built-in default shaders for basic rendering.
/// Written in HLSL - ShaderCross will transpile to SPIRV/MSL/DXIL/DXBC as needed.
/// </summary>
public static class DefaultShaders
{
    /// <summary>
    /// Simple vertex shader in HLSL.
    /// Transforms position and passes through color.
    /// ShaderCross will transpile this to the target GPU backend format.
    /// </summary>
    public const string SimpleVertexShaderHLSL = @"
struct VSInput
{
    float2 Position : POSITION;
    float4 Color : COLOR;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float4 Color : COLOR;
};

cbuffer UniformBuffer : register(b0)
{
    float4x4 Projection;
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.Position = mul(Projection, float4(input.Position, 0.0, 1.0));
    output.Color = input.Color;
    return output;
}
";

    /// <summary>
    /// Simple fragment shader in HLSL.
    /// Outputs the interpolated color.
    /// ShaderCross will transpile this to the target GPU backend format.
    /// </summary>
    public const string SimpleFragmentShaderHLSL = @"
struct PSInput
{
    float4 Position : SV_Position;
    float4 Color : COLOR;
};

float4 main(PSInput input) : SV_Target
{
    return input.Color;
}
";
}