using System.Reflection;
using SDL;

namespace Brine2D.SDL.Graphics;

internal static class SpriteShaders
{
    internal static (byte[] vs, byte[] ps) GetResolveShaders(SDL_GPUShaderFormat format)
        => GetShaderPair("Resolve", format);

    internal static (byte[] vs, byte[] ps) GetShaders(SDL_GPUShaderFormat format)
        => GetShaderPair("Sprite", format);

    private static (byte[] vs, byte[] ps) GetShaderPair(string baseName, SDL_GPUShaderFormat format)
    {
        var ext = ExtForFormat(format);
        var vsFile = $"{baseName}VS.{ext}";
        var psFile = $"{baseName}PS.{ext}";

        // External (override) lookup across common content roots
        var baseDir = AppContext.BaseDirectory;
        var tried = new List<string>(8);

        foreach (var root in CandidateShaderDirs(baseDir))
        {
            var vsPath = Path.Combine(root, vsFile);
            var psPath = Path.Combine(root, psFile);
            tried.Add(vsPath);
            tried.Add(psPath);

            if (File.Exists(vsPath) && File.Exists(psPath))
            {
                return (File.ReadAllBytes(vsPath), File.ReadAllBytes(psPath));
            }
        }

        // Embedded fallback
        if (TryLoadEmbedded(vsFile, out var vsBytes) &&
            TryLoadEmbedded(psFile, out var psBytes))
        {
            return (vsBytes, psBytes);
        }

        // Diagnostics
        var asm = typeof(SpriteShaders).Assembly;
        var manifest = string.Join(", ", asm.GetManifestResourceNames());
        throw new FileNotFoundException(
            $"Missing shader pair for format={format}. Tried files:\n  {string.Join("\n  ", tried)}\n" +
            $"And embedded resources ending with: {vsFile}, {psFile}\nResources present: {manifest}");
    }

    private static IEnumerable<string> CandidateShaderDirs(string baseDir)
    {
        // Prefer Content/Shaders, then content/shaders, Assets/Shaders, assets/shaders
        yield return Path.Combine(baseDir, "Content", "Shaders");
        yield return Path.Combine(baseDir, "content", "shaders");
        yield return Path.Combine(baseDir, "Assets", "Shaders");
        yield return Path.Combine(baseDir, "assets", "shaders");
    }

    private static bool TryLoadEmbedded(string fileName, out byte[] bytes)
    {
        var asm = typeof(SpriteShaders).Assembly;
        var names = asm.GetManifestResourceNames();

        // Exact logical name defined in the project, plus tolerant fallbacks
        var match = names.FirstOrDefault(n => string.Equals(n, $"Content.Shaders.{fileName}", StringComparison.Ordinal))
                 ?? names.FirstOrDefault(n => n.EndsWith($".Content.Shaders.{fileName}", StringComparison.Ordinal))
                 ?? names.FirstOrDefault(n => n.EndsWith(fileName, StringComparison.Ordinal));

        if (match is null)
        {
            bytes = Array.Empty<byte>();
            return false;
        }

        using var s = asm.GetManifestResourceStream(match);
        if (s is null)
        {
            bytes = Array.Empty<byte>();
            return false;
        }

        using var ms = new MemoryStream();
        s.CopyTo(ms);
        bytes = ms.ToArray();
        return true;
    }

    private static string ExtForFormat(SDL_GPUShaderFormat format) => format switch
    {
        SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL => "dxil",
        SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV => "spv",
        SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL => "msl",
        SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_METALLIB => "metallib",
        _ => "bin"
    };
}