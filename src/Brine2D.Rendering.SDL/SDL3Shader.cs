using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// SDL3 GPU shader implementation.
/// </summary>
public class SDL3Shader : IShader
{
    private readonly ILogger<SDL3Shader> _logger;
    private nint _shaderHandle;
    private bool _disposed;

    public string Name { get; }
    public bool IsCompiled { get; private set; }
    public ShaderStage Stage { get; }

    public SDL3Shader(string name, ShaderStage stage, ILogger<SDL3Shader> logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Stage = stage;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Compiles the shader from bytecode.
    /// </summary>
    public bool Compile(nint device, byte[] bytecode, string entryPoint, SDL3.SDL.GPUShaderFormat format = SDL3.SDL.GPUShaderFormat.SPIRV)
    {
        if (IsCompiled)
        {
            _logger.LogWarning("Shader {Name} is already compiled", Name);
            return true;
        }

        var createInfo = new SDL3.SDL.GPUShaderCreateInfo
        {
            Code = Marshal.UnsafeAddrOfPinnedArrayElement(bytecode, 0),
            CodeSize = (nuint)bytecode.Length,
            Entrypoint = entryPoint,
            Format = format,
            Stage = Stage == ShaderStage.Vertex 
                ? SDL3.SDL.GPUShaderStage.Vertex 
                : SDL3.SDL.GPUShaderStage.Fragment,
            
            // Fragment shader: 1 texture sampler (space2)
            // Vertex shader: 1 uniform buffer (space1)
            NumSamplers = Stage == ShaderStage.Fragment ? 1u : 0u,
            NumStorageTextures = 0,
            NumStorageBuffers = 0,
            NumUniformBuffers = Stage == ShaderStage.Vertex ? 1u : 0u
        };

        _shaderHandle = SDL3.SDL.CreateGPUShader(device, ref createInfo);

        if (_shaderHandle == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create shader {Name}: {Error}", Name, error);
            return false;
        }

        IsCompiled = true;
        _logger.LogDebug("Compiled shader: {Name} (format: {Format})", Name, format);
        return true;
    }

    internal nint Handle => _shaderHandle;

    public void SetUniform(string name, float value)
    {
        // Uniforms in SDL3 GPU are set via uniform buffers, not individual calls
        _logger.LogTrace("SetUniform {Name} = {Value}", name, value);
    }

    public void SetUniform(string name, float x, float y)
    {
        _logger.LogTrace("SetUniform {Name} = ({X}, {Y})", name, x, y);
    }

    public void SetUniform(string name, Color color)
    {
        SetUniform(name, color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }

    private void SetUniform(string name, float x, float y, float z, float w)
    {
        _logger.LogTrace("SetUniform {Name} = ({X}, {Y}, {Z}, {W})", name, x, y, z, w);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_shaderHandle != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUShader(nint.Zero, _shaderHandle);
            _shaderHandle = nint.Zero;
        }

        IsCompiled = false;
        _disposed = true;
    }
}