namespace Brine2D.Rendering.SDL.Shaders.PostProcessing;

/// <summary>
/// Pre-compiled blur post-processing shaders.
/// Uses a single fragment shader with uniforms to control blur direction.
/// </summary>
public static class BlurShaders
{
    // Vertex shader (shared)
    public const string VertexShaderSPIRVResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.blur_vertex.spv";
    public const string VertexShaderDXILResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.blur_vertex.dxil";
    public const string VertexShaderDXBCResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.blur_vertex.dxbc";
    public const string VertexShaderMSLResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.blur_vertex.msl";

    // Fragment shader (with uniforms for direction)
    public const string FragmentShaderSPIRVResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.blur_fragment.spv";
    public const string FragmentShaderDXILResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.blur_fragment.dxil";
    public const string FragmentShaderDXBCResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.blur_fragment.dxbc";
    public const string FragmentShaderMSLResourceName = "Brine2D.Rendering.SDL.Shaders.PostProcessing.blur_fragment.msl";

    public static byte[]? LoadVertexShaderSPIRV() => LoadEmbeddedResource(VertexShaderSPIRVResourceName);
    public static byte[]? LoadVertexShaderDXIL() => LoadEmbeddedResource(VertexShaderDXILResourceName);
    public static byte[]? LoadVertexShaderDXBC() => LoadEmbeddedResource(VertexShaderDXBCResourceName);
    public static byte[]? LoadVertexShaderMSL() => LoadEmbeddedResource(VertexShaderMSLResourceName);

    public static byte[]? LoadFragmentShaderSPIRV() => LoadEmbeddedResource(FragmentShaderSPIRVResourceName);
    public static byte[]? LoadFragmentShaderDXIL() => LoadEmbeddedResource(FragmentShaderDXILResourceName);
    public static byte[]? LoadFragmentShaderDXBC() => LoadEmbeddedResource(FragmentShaderDXBCResourceName);
    public static byte[]? LoadFragmentShaderMSL() => LoadEmbeddedResource(FragmentShaderMSLResourceName);

    private static byte[]? LoadEmbeddedResource(string resourceName)
    {
        var assembly = typeof(BlurShaders).Assembly;
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