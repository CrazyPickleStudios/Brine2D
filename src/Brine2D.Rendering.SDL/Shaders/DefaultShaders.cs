namespace Brine2D.Rendering.SDL.Shaders;

/// <summary>
/// Built-in default shaders for basic rendering.
/// HLSL source is provided for reference/documentation.
/// The actual shaders are compiled from vertex.hlsl and fragment.hlsl at build time.
/// </summary>
public static class DefaultShaders
{
    /// <summary>
    /// Simple vertex shader in HLSL (reference only).
    /// The actual shader is compiled from Shaders/vertex.hlsl at build time.
    /// Transforms position and passes through color.
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
    /// Simple fragment shader in HLSL (reference only).
    /// The actual shader is compiled from Shaders/fragment.hlsl at build time.
    /// Outputs the interpolated color.
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
    float4 texColor = Texture.Sample(Sampler, input.TexCoord);
    return texColor * input.Color;
}
";
    
    public const string VertexShaderResourceName = "Brine2D.Rendering.SDL.Shaders.default_vertex.spv";
    public const string FragmentShaderResourceName = "Brine2D.Rendering.SDL.Shaders.default_fragment.spv";

    public static byte[]? LoadVertexShaderSPIRV()
    {
        return LoadEmbeddedResource(VertexShaderResourceName);
    }

    public static byte[]? LoadFragmentShaderSPIRV()
    {
        return LoadEmbeddedResource(FragmentShaderResourceName);
    }
    
    public const string VertexShaderDXILResourceName = "Brine2D.Rendering.SDL.Shaders.default_vertex.dxil";
    public const string FragmentShaderDXILResourceName = "Brine2D.Rendering.SDL.Shaders.default_fragment.dxil";

    public static byte[]? LoadVertexShaderDXIL()
    {
        return LoadEmbeddedResource(VertexShaderDXILResourceName);
    }

    public static byte[]? LoadFragmentShaderDXIL()
    {
        return LoadEmbeddedResource(FragmentShaderDXILResourceName);
    }
    
    public const string VertexShaderDXBCResourceName = "Brine2D.Rendering.SDL.Shaders.default_vertex.dxbc";
    public const string FragmentShaderDXBCResourceName = "Brine2D.Rendering.SDL.Shaders.default_fragment.dxbc";

    public static byte[]? LoadVertexShaderDXBC()
    {
        return LoadEmbeddedResource(VertexShaderDXBCResourceName);
    }

    public static byte[]? LoadFragmentShaderDXBC()
    {
        return LoadEmbeddedResource(FragmentShaderDXBCResourceName);
    }
    
    public const string VertexShaderMSLResourceName = "Brine2D.Rendering.SDL.Shaders.default_vertex.msl";
    public const string FragmentShaderMSLResourceName = "Brine2D.Rendering.SDL.Shaders.default_fragment.msl";

    public static byte[]? LoadVertexShaderMSL()
    {
        return LoadEmbeddedResource(VertexShaderMSLResourceName);
    }

    public static byte[]? LoadFragmentShaderMSL()
    {
        return LoadEmbeddedResource(FragmentShaderMSLResourceName);
    }
    
    private static byte[]? LoadEmbeddedResource(string resourceName)
    {
        var assembly = typeof(DefaultShaders).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        
        if (stream == null)
        {
            return null;
        }

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}