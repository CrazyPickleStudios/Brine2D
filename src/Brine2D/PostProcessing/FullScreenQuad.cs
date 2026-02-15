using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering.SDL.PostProcessing;

/// <summary>
/// Helper for rendering full-screen quads for post-processing effects.
/// </summary>
internal static class FullScreenQuad
{
    /// <summary>
    /// Blit a source texture to a target texture using GPU blit operation.
    /// </summary>
    public static void Blit(nint commandBuffer, nint sourceTexture, nint targetTexture, int width, int height, ILogger? logger = null)
    {
        logger?.LogDebug("FullScreenQuad.Blit: Starting blit");

        var blitInfo = new SDL3.SDL.GPUBlitInfo
        {
            Source = new SDL3.SDL.GPUBlitRegion
            {
                Texture = sourceTexture,
                MipLevel = 0,
                LayerOrDepthPlane = 0,
                X = 0,
                Y = 0,
                W = (uint)width,
                H = (uint)height
            },
            Destination = new SDL3.SDL.GPUBlitRegion
            {
                Texture = targetTexture,
                MipLevel = 0,
                LayerOrDepthPlane = 0,
                X = 0,
                Y = 0,
                W = (uint)width,
                H = (uint)height
            },
            LoadOp = SDL3.SDL.GPULoadOp.DontCare,
            ClearColor = new SDL3.SDL.FColor { R = 0, G = 0, B = 0, A = 1 },
            FlipMode = SDL3.SDL.FlipMode.None,
            Filter = SDL3.SDL.GPUFilter.Linear,
            Cycle = 0
        };

        SDL3.SDL.BlitGPUTexture(commandBuffer, ref blitInfo);
    }

    /// <summary>
    /// Render a full-screen quad with a custom shader pipeline (no uniforms).
    /// </summary>
    public static void RenderWithShader(
        nint commandBuffer,
        nint sourceTexture,
        nint targetTexture,
        nint pipeline,
        nint sampler,
        int width,
        int height,
        ILogger? logger = null)
    {
        logger?.LogDebug("FullScreenQuad.RenderWithShader: Starting");

        var colorTargetInfo = new SDL3.SDL.GPUColorTargetInfo
        {
            Texture = targetTexture,
            MipLevel = 0,
            LayerOrDepthPlane = 0,
            ClearColor = new SDL3.SDL.FColor { R = 0, G = 0, B = 0, A = 1 },
            LoadOp = SDL3.SDL.GPULoadOp.DontCare,
            StoreOp = SDL3.SDL.GPUStoreOp.Store,
            ResolveTexture = IntPtr.Zero,
            ResolveMipLevel = 0,
            ResolveLayer = 0,
            Cycle = false,
            CycleResolveTexture = false
        };

        var colorTargets = new[] { colorTargetInfo };
        var colorTargetHandle = GCHandle.Alloc(colorTargets, GCHandleType.Pinned);

        try
        {
            var renderPass = SDL3.SDL.BeginGPURenderPass(
                commandBuffer,
                colorTargetHandle.AddrOfPinnedObject(),
                1,
                IntPtr.Zero);

            if (renderPass == nint.Zero)
            {
                throw new InvalidOperationException($"Failed to begin render pass: {SDL3.SDL.GetError()}");
            }

            try
            {
                SDL3.SDL.BindGPUGraphicsPipeline(renderPass, pipeline);

                var textureBinding = new SDL3.SDL.GPUTextureSamplerBinding
                {
                    Texture = sourceTexture,
                    Sampler = sampler
                };
                SDL3.SDL.BindGPUFragmentSamplers(renderPass, 0, new[] { textureBinding }, 1);

                var viewport = new SDL3.SDL.GPUViewport
                {
                    X = 0,
                    Y = 0,
                    W = width,
                    H = height,
                    MinDepth = 0.0f,
                    MaxDepth = 1.0f
                };
                SDL3.SDL.SetGPUViewport(renderPass, ref viewport);

                var scissor = new SDL3.SDL.Rect { X = 0, Y = 0, W = width, H = height };
                SDL3.SDL.SetGPUScissor(renderPass, ref scissor);

                SDL3.SDL.DrawGPUPrimitives(renderPass, 3, 1, 0, 0);
            }
            finally
            {
                SDL3.SDL.EndGPURenderPass(renderPass);
            }
        }
        finally
        {
            colorTargetHandle.Free();
        }
    }

    /// <summary>
    /// Render a full-screen quad with a custom shader pipeline and uniforms.
    /// Pushes fragment uniform data AFTER BeginGPURenderPass (SDL3 GPU requirement).
    /// </summary>
    public static void RenderWithShaderAndUniforms<T>(
        nint commandBuffer,
        nint sourceTexture,
        nint targetTexture,
        nint pipeline,
        nint sampler,
        T uniforms,
        int width,
        int height,
        ILogger? logger = null) where T : struct
    {
        logger?.LogDebug("FullScreenQuad.RenderWithShaderAndUniforms: Starting");

        var colorTargetInfo = new SDL3.SDL.GPUColorTargetInfo
        {
            Texture = targetTexture,
            MipLevel = 0,
            LayerOrDepthPlane = 0,
            ClearColor = new SDL3.SDL.FColor { R = 0, G = 0, B = 0, A = 1 },
            LoadOp = SDL3.SDL.GPULoadOp.DontCare,
            StoreOp = SDL3.SDL.GPUStoreOp.Store,
            ResolveTexture = IntPtr.Zero,
            ResolveMipLevel = 0,
            ResolveLayer = 0,
            Cycle = false,
            CycleResolveTexture = false
        };

        var colorTargets = new[] { colorTargetInfo };
        var colorTargetHandle = GCHandle.Alloc(colorTargets, GCHandleType.Pinned);

        try
        {
            // 1. Begin render pass FIRST
            var renderPass = SDL3.SDL.BeginGPURenderPass(
                commandBuffer,
                colorTargetHandle.AddrOfPinnedObject(),
                1,
                IntPtr.Zero);

            if (renderPass == nint.Zero)
            {
                throw new InvalidOperationException($"Failed to begin render pass: {SDL3.SDL.GetError()}");
            }

            try
            {
                // 2. Bind pipeline
                SDL3.SDL.BindGPUGraphicsPipeline(renderPass, pipeline);

                // 3. Push fragment uniforms AFTER BeginGPURenderPass
                unsafe
                {
                    var data = uniforms;
                    var dataPtr = (nint)(&data);
                    SDL3.SDL.PushGPUFragmentUniformData(
                        commandBuffer,
                        0, // slot 0 (matches space3)
                        dataPtr,
                        (uint)Marshal.SizeOf<T>()
                    );
                }

                // 4. Bind texture
                var textureBinding = new SDL3.SDL.GPUTextureSamplerBinding
                {
                    Texture = sourceTexture,
                    Sampler = sampler
                };
                SDL3.SDL.BindGPUFragmentSamplers(renderPass, 0, new[] { textureBinding }, 1);

                // 5. Set viewport and scissor
                var viewport = new SDL3.SDL.GPUViewport
                {
                    X = 0,
                    Y = 0,
                    W = width,
                    H = height,
                    MinDepth = 0.0f,
                    MaxDepth = 1.0f
                };
                SDL3.SDL.SetGPUViewport(renderPass, ref viewport);

                var scissor = new SDL3.SDL.Rect { X = 0, Y = 0, W = width, H = height };
                SDL3.SDL.SetGPUScissor(renderPass, ref scissor);

                // 6. Draw
                SDL3.SDL.DrawGPUPrimitives(renderPass, 3, 1, 0, 0);
            }
            finally
            {
                SDL3.SDL.EndGPURenderPass(renderPass);
            }
        }
        finally
        {
            colorTargetHandle.Free();
        }
    }
}