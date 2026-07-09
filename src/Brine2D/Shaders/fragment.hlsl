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

    // SDF filled circle: TexCoord is in [2,3]×[2,3] when emitted by DrawCircleFilled.
    // Normal texture UVs are always in [0,1] so the threshold is unambiguous.
    // One bounding quad replaces N CPU-computed geometry segments; fwidth() gives
    // sub-pixel anti-aliasing that scales automatically with circle size and camera zoom.
    if (uv.x >= 1.5 && uv.x < 3.5)
    {
        float2 circleUV = uv - 2.0;
        float dist = length(circleUV - 0.5);
        float fw = fwidth(dist) * 0.5;
        float alpha = 1.0 - smoothstep(0.5 - fw, 0.5 + fw, dist);
        if (alpha <= 0.0) discard;
        return float4(input.Color.rgb, input.Color.a * alpha);
    }

    // Font glyph: TexCoord.x >= 3.5 signals a font-atlas quad.
    // The actual UV is encoded in (uv - 4.0) so it maps back to [0,1].
    // Using this explicit sentinel removes the fragile near-white grayscale heuristic.
    if (uv.x >= 3.5)
    {
        float2 fontUV = uv - float2(4.0, 4.0);
        float4 texColor = Texture.Sample(Sampler, fontUV);
        return float4(input.Color.rgb, input.Color.a * texColor.a);
    }

    float4 texColor = Texture.Sample(Sampler, uv);
    return texColor * input.Color;
}
