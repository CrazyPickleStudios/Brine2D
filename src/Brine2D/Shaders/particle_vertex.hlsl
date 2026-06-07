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

    // Scale unit corner to half-extent in local space.
    float2 localPos = input.Corner * input.InstSize;

    // Rotate around the particle center.
    float s, c;
    sincos(input.InstRotation, s, c);
    float2 rotated = float2(
        localPos.x * c - localPos.y * s,
        localPos.x * s + localPos.y * c
    );

    // Translate to world space and project.
    float2 worldPos = input.InstPosition + rotated;
    output.Position = mul(Projection, float4(worldPos, 0.0, 1.0));
    output.Color = input.InstColor;

    // Map corner (-1..1) → (0..1) then lerp across UVRect.
    // UVRect = (u1, v1, u2, v2).  SDF sentinel: (2, 2, 3, 3).
    float2 uvCoord = input.Corner * 0.5 + 0.5;
    output.TexCoord = input.InstUVRect.xy + uvCoord * (input.InstUVRect.zw - input.InstUVRect.xy);

    return output;
}