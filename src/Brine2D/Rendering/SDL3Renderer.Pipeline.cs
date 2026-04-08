using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering;

internal sealed partial class SDL3Renderer
{
    private readonly nint[] _singleFenceBuf = new nint[1];

    private void CreateSamplers()
    {
        _sampler = CreateGPUSampler(SDL3.SDL.GPUFilter.Linear);
        _samplerNearest = CreateGPUSampler(SDL3.SDL.GPUFilter.Nearest);
        _logger.LogDebug("Texture samplers created (Linear + Nearest)");
    }

    private nint CreateGPUSampler(SDL3.SDL.GPUFilter filter)
    {
        var samplerCreateInfo = new SDL3.SDL.GPUSamplerCreateInfo
        {
            MinFilter = filter,
            MagFilter = filter,
            MipmapMode = filter == SDL3.SDL.GPUFilter.Linear
                ? SDL3.SDL.GPUSamplerMipmapMode.Linear
                : SDL3.SDL.GPUSamplerMipmapMode.Nearest,
            AddressModeU = SDL3.SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeV = SDL3.SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeW = SDL3.SDL.GPUSamplerAddressMode.ClampToEdge,
            MipLodBias = 0.0f,
            MaxAnisotropy = 1.0f,
            CompareOp = SDL3.SDL.GPUCompareOp.Never,
            MinLod = 0.0f,
            MaxLod = 1.0f
        };

        var sampler = SDL3.SDL.CreateGPUSampler(_device, ref samplerCreateInfo);
        if (sampler == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create {Filter} sampler: {Error}", filter, error);
            throw new InvalidOperationException($"Failed to create {filter} sampler: {error}");
        }

        return sampler;
    }

    private void CreateWhiteTexture()
    {
        var textureCreateInfo = new SDL3.SDL.GPUTextureCreateInfo
        {
            Type = SDL3.SDL.GPUTextureType.TextureType2D,
            Format = SDL3.SDL.GPUTextureFormat.B8G8R8A8Unorm,
            Usage = SDL3.SDL.GPUTextureUsageFlags.Sampler,
            Width = 1,
            Height = 1,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDL3.SDL.GPUSampleCount.SampleCount1
        };

        _whiteTexture = SDL3.SDL.CreateGPUTexture(_device, ref textureCreateInfo);
        if (_whiteTexture == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create white texture: {Error}", error);
            throw new InvalidOperationException($"Failed to create white texture: {error}");
        }

        var pixelData = new byte[] { 255, 255, 255, 255 };
        UploadTextureDataImmediate(_whiteTexture, pixelData, 1, 1);

        _logger.LogDebug("White texture created");
    }

    private void UploadTextureDataImmediate(nint texture, byte[] pixelData, int width, int height)
    {
        var transferCreateInfo = new SDL3.SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUTransferBufferUsage.Upload,
            Size = (uint)pixelData.Length
        };

        var transferBuffer = SDL3.SDL.CreateGPUTransferBuffer(_device, ref transferCreateInfo);
        if (transferBuffer == nint.Zero)
        {
            throw new InvalidOperationException("Failed to create transfer buffer for texture upload");
        }

        try
        {
            var mappedData = SDL3.SDL.MapGPUTransferBuffer(_device, transferBuffer, false);
            if (mappedData == nint.Zero)
            {
                throw new InvalidOperationException("Failed to map transfer buffer for immediate texture upload");
            }

            Marshal.Copy(pixelData, 0, mappedData, pixelData.Length);
            SDL3.SDL.UnmapGPUTransferBuffer(_device, transferBuffer);

            var uploadCmdBuffer = SDL3.SDL.AcquireGPUCommandBuffer(_device);
            if (uploadCmdBuffer == nint.Zero)
            {
                throw new InvalidOperationException("Failed to acquire command buffer for texture upload");
            }

            var copyPass = SDL3.SDL.BeginGPUCopyPass(uploadCmdBuffer);
            if (copyPass == nint.Zero)
            {
                SDL3.SDL.SubmitGPUCommandBuffer(uploadCmdBuffer);
                throw new InvalidOperationException(
                    $"Failed to begin copy pass for immediate texture upload: {SDL3.SDL.GetError()}");
            }

            var source = new SDL3.SDL.GPUTextureTransferInfo
            {
                TransferBuffer = transferBuffer,
                Offset = 0
            };

            var destination = new SDL3.SDL.GPUTextureRegion
            {
                Texture = texture,
                MipLevel = 0,
                Layer = 0,
                X = 0,
                Y = 0,
                Z = 0,
                W = (uint)width,
                H = (uint)height,
                D = 1
            };

            SDL3.SDL.UploadToGPUTexture(copyPass, ref source, ref destination, false);
            SDL3.SDL.EndGPUCopyPass(copyPass);

            var fence = SDL3.SDL.SubmitGPUCommandBufferAndAcquireFence(uploadCmdBuffer);
            if (fence != nint.Zero)
            {
                _singleFenceBuf[0] = fence;
                SDL3.SDL.WaitForGPUFences(_device, true, _singleFenceBuf, 1);
                SDL3.SDL.ReleaseGPUFence(_device, fence);
            }
            else
            {
                _logger.LogError("Failed to acquire fence for immediate texture upload: {Error}", SDL3.SDL.GetError());
                SDL3.SDL.WaitForGPUIdle(_device);
            }
        }
        finally
        {
            SDL3.SDL.ReleaseGPUTransferBuffer(_device, transferBuffer);
        }
    }

    private void CreateGraphicsPipeline()
    {
        _logger.LogDebug("Creating graphics pipelines for all blend modes");

        foreach (var mode in Enum.GetValues<BlendMode>())
        {
            _blendModePipelines[(int)mode] = CreateGraphicsPipelineForBlendMode(mode, _swapchainFormat);
        }
    }

    private void CreatePostProcessPipelines()
    {
        if (!_renderTargetManager.UsePostProcessing)
            return;

        _postProcessFormat = _renderTargetManager.PostProcessFormat;
        _hasDistinctPostProcessFormat = _postProcessFormat != _swapchainFormat;

        if (!_hasDistinctPostProcessFormat)
            return;

        _logger.LogDebug("Creating post-processing pipelines for format: {Format}", _postProcessFormat);

        foreach (var mode in Enum.GetValues<BlendMode>())
        {
            _postProcessBlendModePipelines[(int)mode] = CreateGraphicsPipelineForBlendMode(mode, _postProcessFormat);
        }

        _logger.LogInformation(
            "Post-processing pipelines created for format {Format} (differs from swapchain format {SwapchainFormat})",
            _postProcessFormat, _swapchainFormat);
    }

    private nint CreateGraphicsPipelineForBlendMode(BlendMode blendMode, SDL3.SDL.GPUTextureFormat targetFormat)
    {
        _logger.LogDebug("Creating graphics pipeline for blend mode: {BlendMode}, format: {Format}", blendMode, targetFormat);

        var vertexShader = (_vertexShader as SDL3Shader)?.Handle ?? nint.Zero;
        var fragmentShader = (_fragmentShader as SDL3Shader)?.Handle ?? nint.Zero;

        if (vertexShader == nint.Zero || fragmentShader == nint.Zero)
        {
            throw new InvalidOperationException("Shaders must be compiled before creating pipeline");
        }

        var vertexAttributes = new SDL3.SDL.GPUVertexAttribute[]
        {
            new() { Location = 0, BufferSlot = 0, Format = SDL3.SDL.GPUVertexElementFormat.Float2, Offset = 0 },
            new() { Location = 1, BufferSlot = 0, Format = SDL3.SDL.GPUVertexElementFormat.Float4, Offset = 8 },
            new() { Location = 2, BufferSlot = 0, Format = SDL3.SDL.GPUVertexElementFormat.Float2, Offset = 24 }
        };

        var vertexBufferDescriptions = new SDL3.SDL.GPUVertexBufferDescription[]
        {
            new()
            {
                Slot = 0,
                Pitch = (uint)SDL3BatchRenderer.VertexSize,
                InputRate = SDL3.SDL.GPUVertexInputRate.Vertex,
                InstanceStepRate = 0
            }
        };

        var blendState = blendMode switch
        {
            BlendMode.Alpha => new SDL3.SDL.GPUColorTargetBlendState
            {
                EnableBlend = true,
                SrcColorBlendFactor = SDL3.SDL.GPUBlendFactor.SrcAlpha,
                DstColorBlendFactor = SDL3.SDL.GPUBlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = SDL3.SDL.GPUBlendOp.Add,
                SrcAlphaBlendFactor = SDL3.SDL.GPUBlendFactor.One,
                DstAlphaBlendFactor = SDL3.SDL.GPUBlendFactor.OneMinusSrcAlpha,
                AlphaBlendOp = SDL3.SDL.GPUBlendOp.Add,
                ColorWriteMask = SDL3.SDL.GPUColorComponentFlags.R |
                               SDL3.SDL.GPUColorComponentFlags.G |
                               SDL3.SDL.GPUColorComponentFlags.B |
                               SDL3.SDL.GPUColorComponentFlags.A
            },

            BlendMode.Additive => new SDL3.SDL.GPUColorTargetBlendState
            {
                EnableBlend = true,
                SrcColorBlendFactor = SDL3.SDL.GPUBlendFactor.SrcAlpha,
                DstColorBlendFactor = SDL3.SDL.GPUBlendFactor.One,
                ColorBlendOp = SDL3.SDL.GPUBlendOp.Add,
                SrcAlphaBlendFactor = SDL3.SDL.GPUBlendFactor.One,
                DstAlphaBlendFactor = SDL3.SDL.GPUBlendFactor.One,
                AlphaBlendOp = SDL3.SDL.GPUBlendOp.Add,
                ColorWriteMask = SDL3.SDL.GPUColorComponentFlags.R |
                               SDL3.SDL.GPUColorComponentFlags.G |
                               SDL3.SDL.GPUColorComponentFlags.B |
                               SDL3.SDL.GPUColorComponentFlags.A
            },

            BlendMode.Multiply => new SDL3.SDL.GPUColorTargetBlendState
            {
                EnableBlend = true,
                SrcColorBlendFactor = SDL3.SDL.GPUBlendFactor.DstColor,
                DstColorBlendFactor = SDL3.SDL.GPUBlendFactor.Zero,
                ColorBlendOp = SDL3.SDL.GPUBlendOp.Add,
                SrcAlphaBlendFactor = SDL3.SDL.GPUBlendFactor.One,
                DstAlphaBlendFactor = SDL3.SDL.GPUBlendFactor.Zero,
                AlphaBlendOp = SDL3.SDL.GPUBlendOp.Add,
                ColorWriteMask = SDL3.SDL.GPUColorComponentFlags.R |
                               SDL3.SDL.GPUColorComponentFlags.G |
                               SDL3.SDL.GPUColorComponentFlags.B |
                               SDL3.SDL.GPUColorComponentFlags.A
            },

            BlendMode.None => new SDL3.SDL.GPUColorTargetBlendState
            {
                EnableBlend = false,
                ColorWriteMask = SDL3.SDL.GPUColorComponentFlags.R |
                               SDL3.SDL.GPUColorComponentFlags.G |
                               SDL3.SDL.GPUColorComponentFlags.B |
                               SDL3.SDL.GPUColorComponentFlags.A
            },

            _ => throw new ArgumentException($"Unsupported blend mode: {blendMode}")
        };

        var colorTargetDescriptions = new SDL3.SDL.GPUColorTargetDescription[]
        {
            new()
            {
                Format = targetFormat,
                BlendState = blendState
            }
        };

        var vertexAttribHandle = GCHandle.Alloc(vertexAttributes, GCHandleType.Pinned);
        var vertexBufferHandle = GCHandle.Alloc(vertexBufferDescriptions, GCHandleType.Pinned);
        var colorTargetHandle = GCHandle.Alloc(colorTargetDescriptions, GCHandleType.Pinned);

        try
        {
            var vertexInputState = new SDL3.SDL.GPUVertexInputState
            {
                VertexBufferDescriptions = vertexBufferHandle.AddrOfPinnedObject(),
                NumVertexBuffers = 1,
                VertexAttributes = vertexAttribHandle.AddrOfPinnedObject(),
                NumVertexAttributes = 3,
            };

            var pipelineCreateInfo = new SDL3.SDL.GPUGraphicsPipelineCreateInfo
            {
                VertexShader = vertexShader,
                FragmentShader = fragmentShader,
                VertexInputState = vertexInputState,
                PrimitiveType = SDL3.SDL.GPUPrimitiveType.TriangleList,
                RasterizerState = new SDL3.SDL.GPURasterizerState
                {
                    FillMode = SDL3.SDL.GPUFillMode.Fill,
                    CullMode = SDL3.SDL.GPUCullMode.None,
                    FrontFace = SDL3.SDL.GPUFrontFace.CounterClockwise
                },
                MultisampleState = new SDL3.SDL.GPUMultisampleState
                {
                    SampleCount = SDL3.SDL.GPUSampleCount.SampleCount1,
                    SampleMask = 0
                },
                DepthStencilState = new SDL3.SDL.GPUDepthStencilState
                {
                    CompareOp = SDL3.SDL.GPUCompareOp.Always,
                    EnableStencilTest = false
                },
                TargetInfo = new SDL3.SDL.GPUGraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions = colorTargetHandle.AddrOfPinnedObject(),
                    NumColorTargets = 1,
                    DepthStencilFormat = SDL3.SDL.GPUTextureFormat.Invalid,
                    HasDepthStencilTarget = false
                }
            };

            var pipeline = SDL3.SDL.CreateGPUGraphicsPipeline(_device, ref pipelineCreateInfo);

            if (pipeline == nint.Zero)
            {
                var error = SDL3.SDL.GetError();
                _logger.LogError("Failed to create graphics pipeline for {BlendMode}: {Error}", blendMode, error);
                throw new InvalidOperationException($"Failed to create graphics pipeline: {error}");
            }

            _logger.LogDebug("Pipeline created for {BlendMode} ({Format})", blendMode, targetFormat);
            return pipeline;
        }
        finally
        {
            vertexAttribHandle.Free();
            vertexBufferHandle.Free();
            colorTargetHandle.Free();
        }
    }

    private void CreateVertexBuffer()
    {
        var bufferCreateInfo = new SDL3.SDL.GPUBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUBufferUsageFlags.Vertex,
            Size = (uint)(SDL3BatchRenderer.VertexSize * _batchRenderer.MaxVertices)
        };

        _vertexBuffer = SDL3.SDL.CreateGPUBuffer(_device, ref bufferCreateInfo);

        if (_vertexBuffer == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create vertex buffer: {Error}", error);
            throw new InvalidOperationException($"Failed to create vertex buffer: {error}");
        }

        _logger.LogDebug("Vertex buffer created ({Size} vertices)", _batchRenderer.MaxVertices);
    }

    public ITexture CreateTextureFromSurface(nint surface, int width, int height, TextureScaleMode scaleMode)
    {
        ThrowIfNotInitialized();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        if (surface == nint.Zero)
            throw new ArgumentException("Surface handle cannot be zero.", nameof(surface));

        var converted = SDL3.SDL.ConvertSurface(surface, SDL3.SDL.PixelFormat.ARGB8888);
        if (converted == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to convert surface to ARGB8888: {Error}", error);
            throw new InvalidOperationException($"Failed to convert surface pixel format: {error}");
        }

        try
        {
            var textureCreateInfo = new SDL3.SDL.GPUTextureCreateInfo
            {
                Type = SDL3.SDL.GPUTextureType.TextureType2D,
                Format = SDL3.SDL.GPUTextureFormat.B8G8R8A8Unorm,
                Usage = SDL3.SDL.GPUTextureUsageFlags.Sampler,
                Width = (uint)width,
                Height = (uint)height,
                LayerCountOrDepth = 1,
                NumLevels = 1,
                SampleCount = SDL3.SDL.GPUSampleCount.SampleCount1
            };

            var gpuTexture = SDL3.SDL.CreateGPUTexture(_device, ref textureCreateInfo);
            if (gpuTexture == nint.Zero)
            {
                var error = SDL3.SDL.GetError();
                _logger.LogError("Failed to create GPU texture from surface: {Error}", error);
                throw new InvalidOperationException($"Failed to create GPU texture: {error}");
            }

            var texture = new SDL3Texture(
                $"surface_{width}x{height}",
                _gpuDeviceHandle!,
                gpuTexture,
                width,
                height,
                scaleMode,
                _loggerFactory.CreateLogger<SDL3Texture>());

            try
            {
                UploadTextureData(texture, converted, width, height, immediate: true);
            }
            catch
            {
                texture.Dispose();
                throw;
            }

            _logger.LogDebug("GPU texture created and uploaded: {Width}x{Height}", width, height);
            return texture;
        }
        finally
        {
            SDL3.SDL.DestroySurface(converted);
        }
    }

    private void UploadTextureData(SDL3Texture texture, nint surface, int width, int height, bool immediate)
    {
        var surfaceStruct = Marshal.PtrToStructure<SDL3.SDL.Surface>(surface);
        var rowBytes = (uint)(width * 4);
        var pixelDataSize = rowBytes * (uint)height;

        var transferCreateInfo = new SDL3.SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUTransferBufferUsage.Upload,
            Size = pixelDataSize
        };

        var transferBuffer = SDL3.SDL.CreateGPUTransferBuffer(_device, ref transferCreateInfo);
        if (transferBuffer == nint.Zero)
        {
            throw new InvalidOperationException("Failed to create texture transfer buffer");
        }

        bool ownsTransferBuffer = true;
        try
        {
            var mappedData = SDL3.SDL.MapGPUTransferBuffer(_device, transferBuffer, false);
            if (mappedData == nint.Zero)
            {
                throw new InvalidOperationException("Failed to map transfer buffer for texture upload");
            }

            unsafe
            {
                if (surfaceStruct.Pitch == (int)rowBytes)
                {
                    Buffer.MemoryCopy(
                        (void*)surfaceStruct.Pixels,
                        (void*)mappedData,
                        pixelDataSize,
                        pixelDataSize);
                }
                else
                {
                    var src = (byte*)surfaceStruct.Pixels;
                    var dst = (byte*)mappedData;
                    for (int row = 0; row < height; row++)
                    {
                        Buffer.MemoryCopy(
                            src + row * surfaceStruct.Pitch,
                            dst + row * rowBytes,
                            rowBytes,
                            rowBytes);
                    }
                }
            }

            SDL3.SDL.UnmapGPUTransferBuffer(_device, transferBuffer);

            var uploadCmdBuffer = SDL3.SDL.AcquireGPUCommandBuffer(_device);
            if (uploadCmdBuffer == nint.Zero)
            {
                throw new InvalidOperationException("Failed to acquire command buffer for texture upload");
            }

            var copyPass = SDL3.SDL.BeginGPUCopyPass(uploadCmdBuffer);
            if (copyPass == nint.Zero)
            {
                var error = SDL3.SDL.GetError();
                _logger.LogError("Failed to begin copy pass for texture upload: {Error}", error);
                SDL3.SDL.SubmitGPUCommandBuffer(uploadCmdBuffer);
                throw new InvalidOperationException($"Failed to begin copy pass for texture upload: {error}");
            }

            var source = new SDL3.SDL.GPUTextureTransferInfo
            {
                TransferBuffer = transferBuffer,
                Offset = 0
            };

            var destination = new SDL3.SDL.GPUTextureRegion
            {
                Texture = texture.Handle,
                MipLevel = 0,
                Layer = 0,
                X = 0,
                Y = 0,
                Z = 0,
                W = (uint)width,
                H = (uint)height,
                D = 1
            };

            SDL3.SDL.UploadToGPUTexture(copyPass, ref source, ref destination, false);
            SDL3.SDL.EndGPUCopyPass(copyPass);

            var deferUpload = !immediate && texture.MarkUploadPending();
            var fence = SDL3.SDL.SubmitGPUCommandBufferAndAcquireFence(uploadCmdBuffer);

            if (fence == nint.Zero)
            {
                _logger.LogError("Failed to acquire fence for texture upload: {Error}", SDL3.SDL.GetError());
                SDL3.SDL.WaitForGPUIdle(_device);
                if (deferUpload) texture.MarkUploadComplete();
                return;
            }

            if (deferUpload)
            {
                _pendingUploads.Add(new PendingTextureUpload(fence, transferBuffer, texture));
                ownsTransferBuffer = false;
            }
            else
            {
                _singleFenceBuf[0] = fence;
                SDL3.SDL.WaitForGPUFences(_device, true, _singleFenceBuf, 1);
                SDL3.SDL.ReleaseGPUFence(_device, fence);
            }
        }
        finally
        {
            if (ownsTransferBuffer)
                SDL3.SDL.ReleaseGPUTransferBuffer(_device, transferBuffer);
        }
    }

    private void PollPendingUploads()
    {
        int writeIdx = 0;

        for (int i = 0; i < _pendingUploads.Count; i++)
        {
            var upload = _pendingUploads[i];
            if (SDL3.SDL.QueryGPUFence(_device, upload.Fence))
            {
                SDL3.SDL.ReleaseGPUFence(_device, upload.Fence);
                SDL3.SDL.ReleaseGPUTransferBuffer(_device, upload.TransferBuffer);

                if (upload.Texture.IsDisposed)
                {
                    upload.Texture.ReleaseDeferredGPUTexture(_device);
                }
                else
                {
                    upload.Texture.MarkUploadComplete();
                    _logger.LogDebug("Deferred texture upload completed: {Name}", upload.Texture.Name);
                }
            }
            else
            {
                _pendingUploads[writeIdx++] = upload;
            }
        }

        _pendingUploads.RemoveRange(writeIdx, _pendingUploads.Count - writeIdx);
    }

    /// <summary>
    /// Releases all pending upload resources unconditionally.
    /// The caller must ensure <see cref="SDL3.SDL.WaitForGPUIdle"/> has completed
    /// before calling this method so that all fences are already signaled.
    /// </summary>
    private void DrainPendingUploads()
    {
        foreach (var upload in _pendingUploads)
        {
            SDL3.SDL.ReleaseGPUFence(_device, upload.Fence);
            SDL3.SDL.ReleaseGPUTransferBuffer(_device, upload.TransferBuffer);

            if (upload.Texture.IsDisposed)
            {
                upload.Texture.ReleaseDeferredGPUTexture(_device);
            }
            else
            {
                upload.Texture.MarkUploadComplete();
            }
        }

        _pendingUploads.Clear();
    }

    public ITexture CreateBlankTexture(int width, int height, TextureScaleMode scaleMode)
    {
        ThrowIfNotInitialized();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var textureCreateInfo = new SDL3.SDL.GPUTextureCreateInfo
        {
            Type = SDL3.SDL.GPUTextureType.TextureType2D,
            Format = SDL3.SDL.GPUTextureFormat.B8G8R8A8Unorm,
            Usage = SDL3.SDL.GPUTextureUsageFlags.Sampler | SDL3.SDL.GPUTextureUsageFlags.ColorTarget,
            Width = (uint)width,
            Height = (uint)height,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDL3.SDL.GPUSampleCount.SampleCount1
        };

        var gpuTexture = SDL3.SDL.CreateGPUTexture(_device, ref textureCreateInfo);
        if (gpuTexture == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create blank GPU texture: {Error}", error);
            throw new InvalidOperationException($"Failed to create GPU texture: {error}");
        }

        ClearTextureImmediate(gpuTexture);

        return new SDL3Texture(
            $"blank_{width}x{height}",
            _gpuDeviceHandle!,
            gpuTexture,
            width,
            height,
            scaleMode,
            _loggerFactory.CreateLogger<SDL3Texture>());
    }

    /// <summary>
    /// Clears a GPU texture to transparent black with a one-shot render pass.
    /// The texture must have <see cref="SDL3.SDL.GPUTextureUsageFlags.ColorTarget"/> usage.
    /// </summary>
    private unsafe void ClearTextureImmediate(nint texture)
    {
        var cmdBuffer = SDL3.SDL.AcquireGPUCommandBuffer(_device);
        if (cmdBuffer == nint.Zero)
        {
            _logger.LogWarning("Failed to acquire command buffer for texture clear");
            return;
        }

        var colorTargetInfo = new SDL3.SDL.GPUColorTargetInfo
        {
            Texture = texture,
            ClearColor = new SDL3.SDL.FColor { R = 0, G = 0, B = 0, A = 0 },
            LoadOp = SDL3.SDL.GPULoadOp.Clear,
            StoreOp = SDL3.SDL.GPUStoreOp.Store
        };

        var renderPass = SDL3.SDL.BeginGPURenderPass(cmdBuffer, (nint)(&colorTargetInfo), 1, nint.Zero);
        if (renderPass != nint.Zero)
            SDL3.SDL.EndGPURenderPass(renderPass);

        var fence = SDL3.SDL.SubmitGPUCommandBufferAndAcquireFence(cmdBuffer);
        if (fence != nint.Zero)
        {
            _singleFenceBuf[0] = fence;
            SDL3.SDL.WaitForGPUFences(_device, true, _singleFenceBuf, 1);
            SDL3.SDL.ReleaseGPUFence(_device, fence);
        }
        else
        {
            SDL3.SDL.WaitForGPUIdle(_device);
        }
    }

    public void ReleaseTexture(ITexture texture)
    {
        if (Volatile.Read(ref _disposed) == 1 || _device == nint.Zero) return;
        texture.Dispose();
    }
}