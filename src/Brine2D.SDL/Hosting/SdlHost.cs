using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Brine2D.Core.Content;
using Brine2D.Core.Content.Loaders;
using Brine2D.Core.Graphics;
using Brine2D.Core.Hosting;
using Brine2D.Core.Input;
using Brine2D.Core.Math;
using Brine2D.Core.Runtime;
using Brine2D.SDL.Content.Loaders;
using Brine2D.SDL.Graphics;
using Brine2D.SDL.Input;
using SDL;
using static SDL.SDL3;

namespace Brine2D.SDL.Hosting;

public sealed unsafe class SdlHost : IGameHost, IEngineContext, IWindow, IRenderer, IKeyboard
{
    private readonly ContentManager _content = new();

    // Gamepads
    private readonly SdlGamepads _gamepads = new();

    // Keyboard input (delegated)
    private readonly SdlKeyboard _keyboard = new();

    // Mouse input
    private readonly SdlMouse _mouse = new();
    private readonly SDL_GPUTextureFormat _sceneFormat = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;

    private readonly SdlSpriteRenderer _sprites;

    private Color _pendingClear = Color.CornflowerBlue;

    private SDL_GPUGraphicsPipeline* _resolvePipeline;
    private SDL_GPUShader* _resolvePS;

    private SDL_GPUSampler* _resolveSampler;
    private SDL_GPUShader* _resolveVS;

    // Offscreen resolve path when swapchain is non-sRGB
    private SDL_GPUTexture* _sceneColor;
    private uint _sceneW, _sceneH;

    private string _title = "Brine2D (SDL3 GPU)";

    public SdlHost()
    {
        _sprites = new SdlSpriteRenderer(this);
    }

    // Expose useful state for custom pipelines/shaders
    public bool BackbufferIsSRGB { get; private set; }
    public int CompositionLength => _keyboard.CompositionLength;
    public int CompositionStart => _keyboard.CompositionStart;
    public string CompositionText => _keyboard.CompositionText;
    public IContentManager Content => _content;
    public IGamepads Gamepads => _gamepads;

    public int Height { get; private set; } = 720;

    // Keep IInput surface, delegate to SdlKeyboard
    public IKeyboard Input => this;

    public bool IsClosing { get; private set; }

    public bool IsComposing => _keyboard.IsComposing;

    // Chord helpers
    public KeyboardModifiers Modifiers => _keyboard.Modifiers;

    public IMouse Mouse => _mouse;
    public IRenderer Renderer => this;
    public SDL_GPUShaderFormat ShaderFormat { get; private set; }

    public ISpriteRenderer Sprites => _sprites;

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            if (WindowPtr != null)
            {
                SDL_SetWindowTitle(WindowPtr, _title);
            }
        }
    }

    // Access to typed text and IME composition (per frame)
    public IReadOnlyList<string> TypedText => _keyboard.TypedThisFrame;

    public int Width { get; private set; } = 1280;

    public IWindow Window => this;
    internal SDL_GPUShaderFormat ActiveShaderFormat => ShaderFormat;

    internal SDL_GPUDevice* Device { get; private set; }

    internal SDL_Window* WindowPtr { get; private set; }

    // Formats a chord like "Ctrl+Shift+S" or "Cmd+Shift+S" on macOS.
    public static string FormatChord(KeyboardModifiers mods, Key key, bool preferPlatformNames = true,
        bool includeSideModifiers = false)
    {
        var parts = new List<string>(6);

        // Platform-aware names
        var ctrl = "Ctrl";
        var super = "Super";
        if (preferPlatformNames)
        {
            if (OperatingSystem.IsMacOS())
            {
                ctrl = "Ctrl"; // mac has both Ctrl and Cmd
                super = "Cmd";
            }
            else if (OperatingSystem.IsWindows())
            {
                super = "Win";
            }
        }

        if (includeSideModifiers)
        {
            if ((mods & KeyboardModifiers.LeftControl) != 0)
            {
                parts.Add($"{ctrl}-L");
            }

            if ((mods & KeyboardModifiers.RightControl) != 0)
            {
                parts.Add($"{ctrl}-R");
            }

            if ((mods & KeyboardModifiers.LeftShift) != 0)
            {
                parts.Add("Shift-L");
            }

            if ((mods & KeyboardModifiers.RightShift) != 0)
            {
                parts.Add("Shift-R");
            }

            if ((mods & KeyboardModifiers.LeftAlt) != 0)
            {
                parts.Add("Alt-L");
            }

            if ((mods & KeyboardModifiers.RightAlt) != 0)
            {
                parts.Add("Alt-R");
            }

            if ((mods & KeyboardModifiers.LeftSuper) != 0)
            {
                parts.Add($"{super}-L");
            }

            if ((mods & KeyboardModifiers.RightSuper) != 0)
            {
                parts.Add($"{super}-R");
            }
        }
        else
        {
            if ((mods & KeyboardModifiers.Control) != 0)
            {
                parts.Add(ctrl);
            }

            if ((mods & KeyboardModifiers.Shift) != 0)
            {
                parts.Add("Shift");
            }

            if ((mods & KeyboardModifiers.Alt) != 0)
            {
                parts.Add("Alt");
            }

            if ((mods & KeyboardModifiers.Super) != 0)
            {
                parts.Add(super);
            }
        }

        // Optional: show lock states
        if ((mods & KeyboardModifiers.CapsLock) != 0)
        {
            parts.Add("Caps");
        }

        if ((mods & KeyboardModifiers.NumLock) != 0)
        {
            parts.Add("Num");
        }

        parts.Add(FormatKeyName(key));
        return string.Join('+', parts);

        static string FormatKeyName(Key k)
        {
            // Provide common short labels; otherwise fallback to enum name.
            return k switch
            {
                Key.Escape => "Esc",
                Key.Enter => "Enter",
                Key.Backspace => "Backspace",
                Key.Tab => "Tab",
                Key.Space => "Space",
                Key.Left => "Left",
                Key.Right => "Right",
                Key.Up => "Up",
                Key.Down => "Down",
                Key.Delete => "Delete",
                Key.Insert => "Insert",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "PageUp",
                Key.PageDown => "PageDown",
                Key.PrintScreen => "PrintScreen",
                Key.Pause => "Pause",
                Key.CapsLock => "CapsLock",
                Key.NumLock => "NumLock",
                Key.ScrollLock => "ScrollLock",
                Key.LeftShift => "LShift",
                Key.RightShift => "RShift",
                Key.LeftControl => "LCtrl",
                Key.RightControl => "RCtrl",
                Key.LeftAlt => "LAlt",
                Key.RightAlt => "RAlt",
                Key.LeftSuper => "LSuper",
                Key.RightSuper => "RSuper",
                Key.Menu => "Menu",
                _ => k.ToString()
            };
        }
    }

    public void Clear(Color color)
    {
        _pendingClear = color;
    }

    public bool IsChordDown(Key key, KeyboardModifiers required)
    {
        return _keyboard.IsChordDown(key, required);
    }

    // IInput implementation delegates to SdlKeyboard
    public bool IsKeyDown(Key key)
    {
        return _keyboard.IsKeyDown(key);
    }

    public void Present()
    {
        if (Device == null || WindowPtr == null)
        {
            return;
        }

        var cmd = SDL_AcquireGPUCommandBuffer(Device);
        if (cmd == null)
        {
            return;
        }

        SDL_GPUTexture* backbuffer = null;
        uint bbWidth = 0, bbHeight = 0;

        var acquired = SDL_AcquireGPUSwapchainTexture(cmd, WindowPtr, &backbuffer, &bbWidth, &bbHeight);
        if (acquired != true || backbuffer == null)
        {
            SDL_SubmitGPUCommandBuffer(cmd);
            return;
        }

        var swapFmt = SDL_GetGPUSwapchainTextureFormat(Device, WindowPtr);
        BackbufferIsSRGB = IsSRGBFormat(swapFmt);

        // Pick render target: backbuffer if sRGB, else offscreen linear
        var colorTargetTex = backbuffer;
        var renderTargetFormat = swapFmt;

        if (!BackbufferIsSRGB)
        {
            EnsureSceneColor(bbWidth, bbHeight);
            colorTargetTex = _sceneColor;
            renderTargetFormat = _sceneFormat;
        }

        SDL_FColor clear;
        clear.r = _pendingClear.R / 255f;
        clear.g = _pendingClear.G / 255f;
        clear.b = _pendingClear.B / 255f;
        clear.a = _pendingClear.A / 255f;

        var sceneTarget = new SDL_GPUColorTargetInfo
        {
            texture = colorTargetTex,
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            clear_color = clear
        };

        var pass = SDL_BeginGPURenderPass(cmd, &sceneTarget, 1, null);
        _sprites.RenderInto(pass, colorTargetTex, bbWidth, bbHeight, renderTargetFormat);
        SDL_EndGPURenderPass(pass);

        if (!BackbufferIsSRGB)
        {
            EnsureResolvePipeline(swapFmt);

            var backTarget = new SDL_GPUColorTargetInfo
            {
                texture = backbuffer,
                load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
                clear_color = clear
            };

            var resolvePass = SDL_BeginGPURenderPass(cmd, &backTarget, 1, null);

            SDL_BindGPUGraphicsPipeline(resolvePass, _resolvePipeline);

            SDL_GPUViewport vp;
            vp.x = 0;
            vp.y = 0;
            vp.w = bbWidth;
            vp.h = bbHeight;
            vp.min_depth = 0;
            vp.max_depth = 1;
            SDL_SetGPUViewport(resolvePass, &vp);

            SDL_GPUTextureSamplerBinding ts;
            ts.texture = _sceneColor;
            ts.sampler = _resolveSampler;
            SDL_BindGPUFragmentSamplers(resolvePass, 0, &ts, 1);

            // Fullscreen triangle (SV_VertexID in resolve VS)
            SDL_DrawGPUPrimitives(resolvePass, 3, 1, 0, 0);

            SDL_EndGPURenderPass(resolvePass);
        }

        SDL_SubmitGPUCommandBuffer(cmd);
    }

    public void Run(IGame game)
    {
        SDL_SetAppMetadata(_title, null, null);

        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD | SDL_InitFlags.SDL_INIT_JOYSTICK))
        {
            throw new InvalidOperationException($"SDL_Init failed: {SDL_GetError()}");
        }

        var winFlags = SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY;
        WindowPtr = SDL_CreateWindow(_title, Width, Height, winFlags);
        if (WindowPtr == null)
        {
            throw new InvalidOperationException($"SDL_CreateWindow failed: {SDL_GetError()}");
        }

        // Attach window to mouse for per-window APIs
        _mouse.AttachWindow(WindowPtr);

        int w, h;
        SDL_GetWindowSize(WindowPtr, &w, &h);
        Width = w;
        Height = h;

        Device = CreateDeviceWithFallback();
        if (Device == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUDevice failed: {SDL_GetError()}");
        }

        if (!SDL_ClaimWindowForGPUDevice(Device, WindowPtr))
        {
            throw new InvalidOperationException($"SDL_ClaimWindowForGPUDevice failed: {SDL_GetError()}");
        }

        ShaderFormat = PickShaderFormatForDevice(Device);

#if DEBUG
        var drvPtr = Unsafe_SDL_GetGPUDeviceDriver(Device);
        var drvName = Marshal.PtrToStringUTF8((nint)drvPtr) ?? "unknown";
        var flags = (uint)SDL_GetGPUShaderFormats(Device);
        Debug.WriteLine($"SDL GPU driver: {drvName}, shader formats: 0x{flags:X}, active: {ShaderFormat}");
#endif

        _content.AddFileProvider(new PhysicalFileProvider("Content", "Assets"));
        _content.AddLoader(new StringLoader());
        _content.AddLoader(new BytesLoader());
        _content.AddLoader(new SdlTextureLoader(this));

        SDL_SetWindowResizable(WindowPtr, true);
        SDL_FlashWindow(WindowPtr, SDL_FlashOperation.SDL_FLASH_BRIEFLY);

        var composition = SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR;
        var mode = SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC;
        if (SDL_WindowSupportsGPUPresentMode(Device, WindowPtr, SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX))
        {
            mode = SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX;
        }

        SDL_SetGPUSwapchainParameters(Device, WindowPtr, composition, mode);

        // Eagerly enumerate currently connected gamepads (in addition to runtime events)
        _gamepads.EnumerateExisting();

        game.Initialize(this);

        var loop = new GameLoop(new GameLoopOptions(60, 6));
        loop.Run(
            gt =>
            {
                _mouse.BeginFrame();
                _keyboard.BeginFrame();
                _gamepads.BeginFrame();

                PumpEvents();

                _mouse.Update(gt.TotalSeconds);
                if (IsClosing)
                {
                    return false;
                }

                game.Update(gt);
                return true;
            },
            gt =>
            {
                game.Draw(gt);
                Present();
            });

        Shutdown();
    }

    public void SetKeyRepeatGeneratesPressed(bool enabled)
    {
        _keyboard.TreatRepeatAsPressed = enabled;
    }

    public void SetSpriteBlendMode(SpriteBlendMode mode)
    {
        _sprites.SetBlendMode(mode);
    }

    // Convenience toggles for sprite rendering
    public void SetSpriteSamplerMode(SpriteSamplerMode mode)
    {
        _sprites.SetSamplerMode(mode);
    }

    // Optional input configuration
    public void SetSuppressKeyRepeat(bool enabled)
    {
        _keyboard.SetSuppressKeyRepeat(enabled);
    }

    // Text input/IME controls
    public void StartTextInput(Rectangle? imeRect = null)
    {
        if (WindowPtr == null)
        {
            return;
        }

        SDL_StartTextInput(WindowPtr);

        if (imeRect.HasValue)
        {
            var r = imeRect.Value;

            // SDL wants window coordinates; we assume the provided rect is already in window coords.
            // Place the caret at the end of the rect by default (offset = width).
            var cursorOffset = r.Width;

            SDL_Rect sdlRect;
            sdlRect.x = r.X;
            sdlRect.y = r.Y;
            sdlRect.w = r.Width;
            sdlRect.h = r.Height;

            SDL_SetTextInputArea(WindowPtr, &sdlRect, cursorOffset);
        }
    }

    // Overload to specify caret offset explicitly (offset is relative to rect.x, in window coords)
    public void StartTextInput(Rectangle imeRect, int caretOffset)
    {
        if (WindowPtr == null)
        {
            return;
        }

        SDL_StartTextInput(WindowPtr);

        SDL_Rect sdlRect;
        sdlRect.x = imeRect.X;
        sdlRect.y = imeRect.Y;
        sdlRect.w = imeRect.Width;
        sdlRect.h = imeRect.Height;

        SDL_SetTextInputArea(WindowPtr, &sdlRect, caretOffset);
    }

    public void StopTextInput()
    {
        if (WindowPtr == null)
        {
            return;
        }

        SDL_StopTextInput(WindowPtr);
    }

    // Check multiple chords in one call. Returns true if any match pressed this frame.
    public bool WasAnyChordPressed(params (Key key, KeyboardModifiers mods)[] chords)
    {
        for (var i = 0; i < chords.Length; i++)
        {
            if (WasChordPressed(chords[i].key, chords[i].mods))
            {
                return true;
            }
        }

        return false;
    }

    public bool WasChordPressed(Key key, KeyboardModifiers required)
    {
        return _keyboard.WasChordPressed(key, required);
    }

    public bool WasKeyPressed(Key key)
    {
        return _keyboard.WasKeyPressed(key);
    }

    public bool WasKeyReleased(Key key)
    {
        return _keyboard.WasKeyReleased(key);
    }

    public bool WasShortcutPressed(Key key)
    {
        var mods = _keyboard.Modifiers;
        if (OperatingSystem.IsMacOS())
        {
            return _keyboard.WasChordPressed(key, KeyboardModifiers.Super) // Cmd
                   && (mods & KeyboardModifiers.Control) == 0; // avoid Ctrl+Cmd+key ambiguity
        }

        return _keyboard.WasChordPressed(key, KeyboardModifiers.Control);
    }

    private static bool IsSRGBFormat(SDL_GPUTextureFormat fmt)
    {
        var name = fmt.ToString();
        return name.IndexOf("SRGB", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static MouseButton MapMouseButton(byte sdlButton)
    {
        return sdlButton switch
        {
            1 => MouseButton.Left,
            3 => MouseButton.Right,
            2 => MouseButton.Middle,
            4 => MouseButton.X1,
            5 => MouseButton.X2,
            _ => MouseButton.Left
        };
    }

    private static SDL_GPUShaderFormat PickShaderFormatForDevice(SDL_GPUDevice* device)
    {
        var flags = (uint)SDL_GetGPUShaderFormats(device);
        if ((flags & SDL_GPU_SHADERFORMAT_DXIL) != 0)
        {
            return SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL;
        }

        if ((flags & SDL_GPU_SHADERFORMAT_SPIRV) != 0)
        {
            return SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV;
        }

        if ((flags & SDL_GPU_SHADERFORMAT_MSL) != 0)
        {
            return SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL;
        }

        if ((flags & SDL_GPU_SHADERFORMAT_METALLIB) != 0)
        {
            return SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_METALLIB;
        }

        throw new InvalidOperationException($"Device reports no supported shader formats. Flags=0x{flags:X8}");
    }

    private static byte[] ToUtf8(string s)
    {
        return Encoding.UTF8.GetBytes(s + "\0");
    }

    private SDL_GPUDevice* CreateDeviceWithFallback()
    {
        var want = (SDL_GPUShaderFormat)SDL_GPU_SHADERFORMAT_DXIL;
#if DEBUG
        var debug = (SDLBool)true;
#else
        var debug = (SDLBool)false;
#endif
        var count = SDL_GetNumGPUDrivers();
        for (var i = 0; i < count; i++)
        {
            var namePtr = Unsafe_SDL_GetGPUDriver(i);
            var name = Marshal.PtrToStringUTF8((nint)namePtr);
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var nameBytes = Encoding.UTF8.GetBytes(name + "\0");
            fixed (byte* pName = nameBytes)
            {
                var supports = SDL_GPUSupportsShaderFormats(want, pName) == true;
                if (!supports)
                {
                    continue;
                }

                fixed (byte* pDriver = nameBytes)
                {
                    var dev = SDL_CreateGPUDevice(want, debug, pDriver);
                    if (dev != null)
                    {
                        return dev;
                    }
                }
            }
        }

        var autoDev = SDL_CreateGPUDevice(want, debug, (byte*)null);
        if (autoDev != null)
        {
            return autoDev;
        }

        return SDL_CreateGPUDevice((SDL_GPUShaderFormat)SDL_GPU_SHADERFORMAT_PRIVATE, debug, (byte*)null);
    }

    private void EnsureResolvePipeline(SDL_GPUTextureFormat swapchainFormat)
    {
        if (_resolveSampler == null)
        {
            SDL_GPUSamplerCreateInfo sci = default;
            sci.min_filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
            sci.mag_filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
            sci.mipmap_mode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR;
            sci.address_mode_u = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
            sci.address_mode_v = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
            sci.address_mode_w = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
            sci.enable_compare = false;
            _resolveSampler = SDL_CreateGPUSampler(Device, &sci);
            if (_resolveSampler == null)
            {
                throw new InvalidOperationException($"SDL_CreateGPUSampler (resolve) failed: {SDL_GetError()}");
            }
        }

        if (_resolvePipeline != null)
        {
            return;
        }

        var fmt = ShaderFormat;
        var (vsBytes, psBytes) = SpriteShaders.GetResolveShaders(fmt);

        fixed (byte* vsCode = vsBytes)
        fixed (byte* psCode = psBytes)
        {
            var vsEntry = ToUtf8("VSMain");
            var psEntry = ToUtf8("PSMain");
            fixed (byte* pVsEntry = vsEntry)
            fixed (byte* pPsEntry = psEntry)
            {
                SDL_GPUShaderCreateInfo vsci = default;
                vsci.code = vsCode;
                vsci.code_size = (nuint)vsBytes.Length;
                vsci.entrypoint = pVsEntry;
                vsci.stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX;
                vsci.format = fmt;

                _resolveVS = SDL_CreateGPUShader(Device, &vsci);
                if (_resolveVS == null)
                {
                    throw new InvalidOperationException($"SDL_CreateGPUShader (Resolve VS) failed: {SDL_GetError()}");
                }

                SDL_GPUShaderCreateInfo psci = default;
                psci.code = psCode;
                psci.code_size = (nuint)psBytes.Length;
                psci.entrypoint = pPsEntry;
                psci.stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT;
                psci.format = fmt;
                psci.num_samplers = 1;

                _resolvePS = SDL_CreateGPUShader(Device, &psci);
                if (_resolvePS == null)
                {
                    throw new InvalidOperationException($"SDL_CreateGPUShader (Resolve PS) failed: {SDL_GetError()}");
                }
            }
        }

        SDL_GPUVertexInputState vis = default;

        SDL_GPURasterizerState rast = default;
        rast.fill_mode = SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL;
        rast.cull_mode = SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE;
        rast.front_face = SDL_GPUFrontFace.SDL_GPU_FRONTFACE_COUNTER_CLOCKWISE;

        SDL_GPUMultisampleState ms = default;
        ms.sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1;

        SDL_GPUDepthStencilState ds = default;
        ds.enable_depth_test = false;
        ds.enable_depth_write = false;

        var ct = stackalloc SDL_GPUColorTargetDescription[1];
        ct[0] = new SDL_GPUColorTargetDescription
        {
            format = swapchainFormat,
            blend_state = new SDL_GPUColorTargetBlendState
            {
                enable_blend = false,
                enable_color_write_mask = true,
                color_write_mask = (SDL_GPUColorComponentFlags)(SDL_GPU_COLORCOMPONENT_R | SDL_GPU_COLORCOMPONENT_G |
                                                                SDL_GPU_COLORCOMPONENT_B | SDL_GPU_COLORCOMPONENT_A)
            }
        };

        SDL_GPUGraphicsPipelineTargetInfo pti = default;
        pti.color_target_descriptions = ct;
        pti.num_color_targets = 1;
        pti.has_depth_stencil_target = false;

        SDL_GPUGraphicsPipelineCreateInfo pci = default;
        pci.vertex_shader = _resolveVS;
        pci.fragment_shader = _resolvePS;
        pci.vertex_input_state = vis;
        pci.primitive_type = SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST;
        pci.rasterizer_state = rast;
        pci.multisample_state = ms;
        pci.depth_stencil_state = ds;
        pci.target_info = pti;

        _resolvePipeline = SDL_CreateGPUGraphicsPipeline(Device, &pci);
        if (_resolvePipeline == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUGraphicsPipeline (resolve) failed: {SDL_GetError()}");
        }
    }

    private void EnsureSceneColor(uint w, uint h)
    {
        if (_sceneColor != null && _sceneW == w && _sceneH == h)
        {
            return;
        }

        if (_sceneColor != null)
        {
            SDL_ReleaseGPUTexture(Device, _sceneColor);
            _sceneColor = null;
            _sceneW = _sceneH = 0;
        }

        SDL_GPUTextureCreateInfo ci = default;
        ci.type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D;
        ci.format = _sceneFormat; // linear UNORM
        ci.width = w;
        ci.height = h;
        ci.layer_count_or_depth = 1;
        ci.num_levels = 1;
        ci.sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1;
        ci.usage = (SDL_GPUTextureUsageFlags)(SDL_GPU_TEXTUREUSAGE_COLOR_TARGET | SDL_GPU_TEXTUREUSAGE_SAMPLER);

        _sceneColor = SDL_CreateGPUTexture(Device, &ci);
        if (_sceneColor == null)
        {
            throw new InvalidOperationException($"Failed to create scene color: {SDL_GetError()}");
        }

        _sceneW = w;
        _sceneH = h;
    }

    private void PumpEvents()
    {
        SDL_UpdateGamepads();

        SDL_Event ev;

        while (SDL_PollEvent(&ev))
        {
            switch (ev.Type)
            {
                case SDL_EventType.SDL_EVENT_QUIT:
                case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                    IsClosing = true;
                    break;

                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    _keyboard.OnKeyDown(ev.key.scancode, Convert.ToBoolean(ev.key.repeat));
                    _keyboard.SyncModifiersFromSDL(SDL_GetModState());
                    if (_keyboard.IsKeyDown(Key.Escape))
                    {
                        IsClosing = true;
                    }

                    break;

                case SDL_EventType.SDL_EVENT_KEY_UP:
                    _keyboard.OnKeyUp(ev.key.scancode);
                    _keyboard.SyncModifiersFromSDL(SDL_GetModState());
                    break;

                case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                {
                    var txt = ev.text.text != null ? Marshal.PtrToStringUTF8((nint)ev.text.text) : null;
                    if (!string.IsNullOrEmpty(txt))
                    {
                        _keyboard.OnTextInput(txt);
                    }

                    break;
                }

                case SDL_EventType.SDL_EVENT_TEXT_EDITING:
                {
                    var txt = ev.edit.text != null ? Marshal.PtrToStringUTF8((nint)ev.edit.text) : string.Empty;
                    _keyboard.OnTextEditing(txt ?? string.Empty, ev.edit.start, ev.edit.length);
                    break;
                }

                case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
                    int w, h;
                    SDL_GetWindowSizeInPixels(WindowPtr, &w, &h);
                    Width = w;
                    Height = h;
                    _mouse.OnWindowResized();
                    break;

                case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
                    _mouse.OnEnterWindow();
                    break;

                case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                    _mouse.OnLeaveWindow();
                    break;

                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                    _mouse.OnFocusLost();
                    _keyboard.OnFocusLost();
                    _gamepads.OnFocusLost();
                    break;

                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
                    _mouse.OnFocusGained();
                    _keyboard.OnFocusGained();
                    _gamepads.OnFocusGained();
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                    _mouse.OnMotion(ev.motion.x, ev.motion.y, ev.motion.xrel, ev.motion.yrel, ev.motion.which);
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                    _mouse.OnWheel(ev.wheel.x, ev.wheel.y, ev.wheel.which);
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                {
                    var b = MapMouseButton(ev.button.button);
                    _mouse.OnButtonDown(b, ev.button.clicks, ev.button.which);
                    break;
                }

                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                {
                    var b = MapMouseButton(ev.button.button);
                    _mouse.OnButtonUp(b, ev.button.which);
                    break;
                }

                // Gamepad hotplug and input
                case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                    _gamepads.OnGamepadAdded(ev.gdevice.which);
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                    _gamepads.OnGamepadRemoved(ev.gdevice.which);
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
                    _gamepads.OnGamepadButton(ev.gbutton.which, ev.gbutton.Button, true);
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
                    _gamepads.OnGamepadButton(ev.gbutton.which, ev.gbutton.Button, false);
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
                    _gamepads.OnGamepadAxis(ev.gaxis.which, ev.gaxis.Axis, ev.gaxis.value);
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_REMAPPED:
                    _gamepads.OnGamepadRemapped(ev.gdevice.which);
                    break;
            }
        }
    }

    private void Shutdown()
    {
        try
        {
            _sprites.Dispose();

            // Dispose mouse resources (cursor cache)
            _mouse.Dispose();

            // Close gamepads
            _gamepads.Dispose();

            if (_resolvePipeline != null)
            {
                SDL_ReleaseGPUGraphicsPipeline(Device, _resolvePipeline);
                _resolvePipeline = null;
            }

            if (_resolvePS != null)
            {
                SDL_ReleaseGPUShader(Device, _resolvePS);
                _resolvePS = null;
            }

            if (_resolveVS != null)
            {
                SDL_ReleaseGPUShader(Device, _resolveVS);
                _resolveVS = null;
            }

            if (_resolveSampler != null)
            {
                SDL_ReleaseGPUSampler(Device, _resolveSampler);
                _resolveSampler = null;
            }

            if (_sceneColor != null)
            {
                SDL_ReleaseGPUTexture(Device, _sceneColor);
                _sceneColor = null;
                _sceneW = _sceneH = 0;
            }

            if (Device != null && WindowPtr != null)
            {
                SDL_ReleaseWindowFromGPUDevice(Device, WindowPtr);
            }

            if (Device != null)
            {
                SDL_DestroyGPUDevice(Device);
                Device = null;
            }

            if (WindowPtr != null)
            {
                SDL_DestroyWindow(WindowPtr);
                WindowPtr = null;
            }
        }
        finally
        {
            SDL_Quit();
        }
    }
}