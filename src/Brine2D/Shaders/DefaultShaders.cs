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
    /// Handles textured quads, font rendering, and SDF circles.
    /// UV sentinel ranges:
    /// [0,1]   — normal texture/sprite quad (sample UVs directly).
    /// [2,3]   — SDF filled circle (TexCoord emitted by DrawCircleFilled).
    /// [4,5]   — font atlas glyph (TexCoord = actual UV + (4,4), decoded here).
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

    if (uv.x >= 1.5 && uv.x < 3.5)
    {
        float2 circleUV = uv - 2.0;
        float dist = length(circleUV - 0.5);
        float fw = fwidth(dist) * 0.5;
        float alpha = 1.0 - smoothstep(0.5 - fw, 0.5 + fw, dist);
        if (alpha <= 0.0) discard;
        return float4(input.Color.rgb, input.Color.a * alpha);
    }

    if (uv.x >= 3.5)
    {
        float2 fontUV = uv - float2(4.0, 4.0);
        float4 texColor = Texture.Sample(Sampler, fontUV);
        return float4(input.Color.rgb, input.Color.a * texColor.a);
    }

    float4 texColor = Texture.Sample(Sampler, uv);
    return texColor * input.Color;
}
";
}
