namespace Brine2D.Rendering.SDL.Shaders;

/// <summary>
/// Built-in default shaders for basic rendering.
/// Compiled at runtime via SDL_ShaderCross.
/// </summary>
public static class DefaultShaders
{
    /// <summary>
    /// Vertex shader HLSL source. Transforms position and passes through color.
    /// </summary>
    public const string SimpleVertexShaderHLSL = @"
struct VSInput
{
    float2 Position : TEXCOORD0;
    float4 Color    : TEXCOORD1;
    float2 TexCoord : TEXCOORD2;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float4 Color    : COLOR;
    float2 TexCoord : TEXCOORD0;
};

cbuffer VertexUniforms : register(b0, space1)
{
    float4x4 Projection;
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.Position = mul(Projection, float4(input.Position, 0.0, 1.0));
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    return output;
}
";

    /// <summary>
    /// Fragment shader HLSL source.
    /// Handles textured quads, font rendering, and SDF circles (TexCoord in [2,3] sentinel).
    /// </summary>
    public const string SimpleFragmentShaderHLSL = @"
struct PSInput
{
    float4 Position : SV_Position;
    float4 Color    : COLOR;
    float2 TexCoord : TEXCOORD0;
};

Texture2D Texture : register(t0, space2);
SamplerState Sampler : register(s0, space2);

float4 main(PSInput input) : SV_Target
{
    float2 uv = input.TexCoord;

    if (uv.x >= 1.5)
    {
        float2 circleUV = uv - 2.0;
        float dist = length(circleUV - 0.5);
        float fw = fwidth(dist) * 0.5;
        float alpha = 1.0 - smoothstep(0.5 - fw, 0.5 + fw, dist);
        if (alpha <= 0.0) discard;
        return float4(input.Color.rgb, input.Color.a * alpha);
    }

    float4 texColor = Texture.Sample(Sampler, uv);

    float grayscale = (texColor.r + texColor.g + texColor.b) / 3.0;
    bool isFont = grayscale > 0.95 &&
                  abs(texColor.r - texColor.g) < 0.01 &&
                  abs(texColor.g - texColor.b) < 0.01;

    if (isFont)
    {
        return float4(input.Color.rgb, input.Color.a * texColor.a);
    }

    return texColor * input.Color;
}
";
}
