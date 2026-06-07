namespace Brine2D.Rendering.SDL.Shaders;

/// <summary>
/// Built-in shaders for hardware-instanced particle rendering.
/// Compiled at runtime via SDL_ShaderCross.
/// </summary>
/// <remarks>
/// <para>
/// The vertex shader uses two vertex buffer slots:
/// slot 0 — a static per-vertex unit quad (4 × <c>float2 Corner</c>, pitch = 8 bytes);
/// slot 1 — a per-instance buffer of <c>ParticleInstance</c> records (48 bytes each).
/// </para>
/// <para>
/// Instance layout (48 bytes, matches <c>SDL3ParticleRenderer.ParticleInstance</c>):
/// <list type="bullet">
/// <item>float2 Position (offset 0)</item>
/// <item>float  Size     (offset 8)</item>
/// <item>float  Rotation (offset 12)</item>
/// <item>float4 Color    (offset 16)</item>
/// <item>float4 UVRect   (offset 32) — set to (2,2,3,3) for SDF circle</item>
/// </list>
/// </para>
/// </remarks>
internal static class ParticleShaders
{
    public const string ParticleVertexShaderHLSL = @"
// Slot 0: per-vertex unit-quad corner. Static buffer, never changes.
// Slot 1: per-instance particle data, uploaded once per emitter draw call.
//
// Corner values fed from the static quad VB: (-1,-1), (1,-1), (-1,1), (1,1)
// Index pattern: [0, 1, 2, 1, 3, 2]  (same quad winding as SDL3BatchRenderer)

struct VSInput
{
    float2 Corner       : TEXCOORD0; // slot 0 — per-vertex

    float2 InstPosition : TEXCOORD1; // slot 1 — per-instance
    float  InstSize     : TEXCOORD2;
    float  InstRotation : TEXCOORD3;
    float4 InstColor    : TEXCOORD4;
    float4 InstUVRect   : TEXCOORD5; // (u1,v1,u2,v2); (2,2,3,3) = SDF circle
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

    float2 localPos = input.Corner * input.InstSize;

    float s, c;
    sincos(input.InstRotation, s, c);
    float2 rotated = float2(
        localPos.x * c - localPos.y * s,
        localPos.x * s + localPos.y * c
    );

    float2 worldPos = input.InstPosition + rotated;
    output.Position = mul(Projection, float4(worldPos, 0.0, 1.0));
    output.Color = input.InstColor;

    float2 uvCoord = input.Corner * 0.5 + 0.5;
    output.TexCoord = input.InstUVRect.xy + uvCoord * (input.InstUVRect.zw - input.InstUVRect.xy);

    return output;
}
";

    public const string ParticleFragmentShaderHLSL = @"
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

    // SDF filled circle: UVRect sentinel (2,2,3,3) maps TexCoord into [2,3].
    // Identical logic to the default fragment shader so visual output matches DrawCircleFilled.
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
    return texColor * input.Color;
}
";
}
