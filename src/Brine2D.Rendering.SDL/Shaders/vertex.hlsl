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