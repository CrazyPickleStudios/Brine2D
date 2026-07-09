using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering;

internal sealed partial class SDL3Renderer
{
    /// <inheritdoc />
    public async Task<byte[]> ReadPixelsAsync(ITexture texture, CancellationToken cancellationToken = default)
    {
        ThrowIfNotInitialized();
        ArgumentNullException.ThrowIfNull(texture);

        if (!texture.IsLoaded)
            throw new InvalidOperationException($"Texture '{texture.Name}' is not loaded.");

        if (texture.Width <= 0 || texture.Height <= 0)
            throw new InvalidOperationException($"Texture '{texture.Name}' has invalid dimensions {texture.Width}x{texture.Height}.");

        nint gpuTexture = GetTextureHandle(texture);
        if (gpuTexture == nint.Zero)
            throw new InvalidOperationException($"Texture '{texture.Name}' has a zero GPU handle.");

        int width = texture.Width;
        int height = texture.Height;
        int bytesPerPixel = 4;
        uint bufferSize = (uint)(width * height * bytesPerPixel);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var transferCreateInfo = new SDL3.SDL.GPUTransferBufferCreateInfo
            {
                Usage = SDL3.SDL.GPUTransferBufferUsage.Download,
                Size = bufferSize
            };

            var transferBuffer = SDL3.SDL.CreateGPUTransferBuffer(_device, ref transferCreateInfo);
            if (transferBuffer == nint.Zero)
                throw new InvalidOperationException(
                    $"ReadPixelsAsync: failed to create download transfer buffer: {SDL3.SDL.GetError()}");

            try
            {
                var cmdBuffer = SDL3.SDL.AcquireGPUCommandBuffer(_device);
                if (cmdBuffer == nint.Zero)
                    throw new InvalidOperationException(
                        $"ReadPixelsAsync: failed to acquire command buffer: {SDL3.SDL.GetError()}");

                var copyPass = SDL3.SDL.BeginGPUCopyPass(cmdBuffer);
                if (copyPass == nint.Zero)
                {
                    SDL3.SDL.SubmitGPUCommandBuffer(cmdBuffer);
                    throw new InvalidOperationException(
                        $"ReadPixelsAsync: failed to begin copy pass: {SDL3.SDL.GetError()}");
                }

                var source = new SDL3.SDL.GPUTextureRegion
                {
                    Texture = gpuTexture,
                    MipLevel = 0,
                    Layer = 0,
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = (uint)width,
                    H = (uint)height,
                    D = 1
                };

                var destination = new SDL3.SDL.GPUTextureTransferInfo
                {
                    TransferBuffer = transferBuffer,
                    Offset = 0
                };

                SDL3.SDL.DownloadFromGPUTexture(copyPass, ref source, ref destination);
                SDL3.SDL.EndGPUCopyPass(copyPass);

                var fence = SDL3.SDL.SubmitGPUCommandBufferAndAcquireFence(cmdBuffer);
                if (fence == nint.Zero)
                {
                    _logger.LogError("ReadPixelsAsync: failed to acquire fence: {Error}", SDL3.SDL.GetError());
                    SDL3.SDL.WaitForGPUIdle(_device);
                }
                else
                {
                    nint[] fenceBuf = [fence];
                    SDL3.SDL.WaitForGPUFences(_device, true, fenceBuf, 1);
                    SDL3.SDL.ReleaseGPUFence(_device, fence);
                }

                cancellationToken.ThrowIfCancellationRequested();

                var mapped = SDL3.SDL.MapGPUTransferBuffer(_device, transferBuffer, false);
                if (mapped == nint.Zero)
                    throw new InvalidOperationException(
                        $"ReadPixelsAsync: failed to map download transfer buffer: {SDL3.SDL.GetError()}");

                var pixels = new byte[bufferSize];
                Marshal.Copy(mapped, pixels, 0, (int)bufferSize);
                SDL3.SDL.UnmapGPUTransferBuffer(_device, transferBuffer);

                _logger.LogDebug("ReadPixelsAsync: downloaded {Bytes} bytes from '{Name}' ({W}x{H})",
                    bufferSize, texture.Name, width, height);

                return pixels;
            }
            finally
            {
                SDL3.SDL.ReleaseGPUTransferBuffer(_device, transferBuffer);
            }
        }, cancellationToken);
    }
}
