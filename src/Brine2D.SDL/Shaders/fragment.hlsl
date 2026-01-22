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
    float4 texColor = Texture.Sample(Sampler, input.TexCoord);
    
    // Check if this is a white/grayscale texture (like fonts)
    // Average the RGB to get grayscale value
    float grayscale = (texColor.r + texColor.g + texColor.b) / 3.0;
    
    // Detect if texture is pure white/gray (font) vs colored (regular texture)
    bool isFont = grayscale > 0.95 && 
                  abs(texColor.r - texColor.g) < 0.01 && 
                  abs(texColor.g - texColor.b) < 0.01;
    
    if (isFont)
    {
        // Font rendering: use white texture as alpha mask
        return float4(input.Color.rgb, input.Color.a * texColor.a);
    }
    else
    {
        // Regular texture rendering
        return texColor * input.Color;
    }
}