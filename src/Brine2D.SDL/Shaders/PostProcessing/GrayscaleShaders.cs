namespace Brine2D.Rendering.SDL.Shaders.PostProcessing;

/// <summary>
/// Pre-compiled grayscale post-processing shaders.
/// Shaders are compiled from HLSL source at build time and embedded as resources.
/// </summary>
public static class GrayscaleShaders
{
    // SPIRV (for Vulkan)
    public const string VertexShaderSPIRVResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.grayscale_vertex.spv";
    public const string FragmentShaderSPIRVResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.grayscale_fragment.spv";

    public static byte[]? LoadVertexShaderSPIRV()
    {
        return LoadEmbeddedResource(VertexShaderSPIRVResourceName);
    }

    public static byte[]? LoadFragmentShaderSPIRV()
    {
        return LoadEmbeddedResource(FragmentShaderSPIRVResourceName);
    }

    // DXIL (for Direct3D 12)
    public const string VertexShaderDXILResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.grayscale_vertex.dxil";
    public const string FragmentShaderDXILResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.grayscale_fragment.dxil";

    public static byte[]? LoadVertexShaderDXIL()
    {
        return LoadEmbeddedResource(VertexShaderDXILResourceName);
    }

    public static byte[]? LoadFragmentShaderDXIL()
    {
        return LoadEmbeddedResource(FragmentShaderDXILResourceName);
    }

    // DXBC (for Direct3D 11 - if needed)
    public const string VertexShaderDXBCResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.grayscale_vertex.dxbc";
    public const string FragmentShaderDXBCResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.grayscale_fragment.dxbc";

    public static byte[]? LoadVertexShaderDXBC()
    {
        return LoadEmbeddedResource(VertexShaderDXBCResourceName);
    }

    public static byte[]? LoadFragmentShaderDXBC()
    {
        return LoadEmbeddedResource(FragmentShaderDXBCResourceName);
    }

    // MSL (for Metal on macOS/iOS)
    public const string VertexShaderMSLResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.grayscale_vertex.msl";
    public const string FragmentShaderMSLResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.grayscale_fragment.msl";

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
        var assembly = typeof(GrayscaleShaders).Assembly;
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