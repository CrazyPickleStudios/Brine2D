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
    // Identical logic to fragment.hlsl so instanced circles match DrawCircleFilled visually.
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