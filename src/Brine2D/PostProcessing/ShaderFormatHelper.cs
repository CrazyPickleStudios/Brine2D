namespace Brine2D.Rendering.SDL.PostProcessing;

/// <summary>
/// Resolves SDL3 GPU driver names to shader target formats.
/// </summary>
internal static class ShaderFormatHelper
{
    internal enum GraphicsShaderTarget
    {
        SPIRV,
        DXIL,
        DXBC,
        MSL
    }

    internal static GraphicsShaderTarget GetTargetFromDriver(string driverName)
    {
        return driverName.ToLowerInvariant() switch
        {
            "vulkan" => GraphicsShaderTarget.SPIRV,
            "direct3d12" or "d3d12" => GraphicsShaderTarget.DXIL,
            "direct3d11" or "d3d11" => GraphicsShaderTarget.DXBC,
            "metal" => GraphicsShaderTarget.MSL,
            _ => GraphicsShaderTarget.SPIRV
        };
    }

    internal static SDL3.SDL.GPUShaderFormat GetShaderFormat(GraphicsShaderTarget target)
    {
        return target switch
        {
            GraphicsShaderTarget.SPIRV => SDL3.SDL.GPUShaderFormat.SPIRV,
            GraphicsShaderTarget.DXIL => SDL3.SDL.GPUShaderFormat.DXIL,
            GraphicsShaderTarget.DXBC => SDL3.SDL.GPUShaderFormat.DXBC,
            GraphicsShaderTarget.MSL => SDL3.SDL.GPUShaderFormat.MSL,
            _ => SDL3.SDL.GPUShaderFormat.SPIRV
        };
    }
}