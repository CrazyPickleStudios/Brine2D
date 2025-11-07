<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="docs/media/brine2d-logo.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/media/brine2d-logo.svg">
    <img alt="Brine2D" src="docs/media/brine2d-logo.png" width="420" height="auto">
  </picture>
</p>

# Brine2D

Brine2D is a fast, minimal 2D engine built on SDL3’s new GPU API, written entirely in C# for .NET 8. It targets modern backends (D3D12, Vulkan, Metal) via SDL3 GPU and provides a straightforward game loop, sprite renderer, input system, and content loading.

## Highlights
- SDL3 GPU renderer with device-appropriate shaders (DXIL, SPIR-V, MSL/Metallib)
- Immediate sprite API with batching, blend/sampler modes, and camera support
- Robust input over keyboard/mouse/gamepad with actions, axes, sets, repeats, smoothing, JSON save/merge/diff
- Text input and IME composition support
- Simple content system with pluggable providers/loaders
- Cross-platform: Windows, Linux, macOS (via SDL3)

## Requirements
- .NET 8 SDK
- Visual Studio 2022 or the dotnet CLI
- SDL3 native runtime accessible at runtime (SDL3.dll / libSDL3.so / libSDL3.dylib)
- DirectX Shader Compiler (dxc) on PATH to build engine shaders
  - Windows: install “DirectX Shader Compiler” or Vulkan SDK (includes dxc). Verify with: where dxc
  - macOS/Linux: install Vulkan SDK and ensure $VULKAN_SDK/bin is on PATH. Verify with: which dxc
  - Alternatively set an absolute path in Brine2D.SDL.csproj:
    <PropertyGroup><DxcExe>C:\Tools\dxc\dxc.exe</DxcExe></PropertyGroup>

## Quick start
1) Build and run the desktop sample in Visual Studio:
   - Set Brine2D.Sample.Desktop as startup project
   - Start Debugging (F5)

2) CLI:
    ```bash
    dotnet run -p samples/Brine2D.Sample.Desktop
    ```

### Minimal game (conceptual)

```csharp
using Brine2D.Core.Graphics;
using Brine2D.Core.Hosting;
using Brine2D.Core.Runtime;
using Brine2D.Core.Timing;
using Brine2D.Core.Math;
using Brine2D.SDL.Hosting;

public sealed class BasicGame : IGame
{
    private IEngineContext _ctx;
    private ITexture2D _tex;

    public void Initialize(IEngineContext context)
    {
        _ctx = context;
        _ctx.Window.Title = "Brine2D - Hello";
        _tex = _ctx.Content.Load<ITexture2D>("images/player.png");
    }

    public void Update(GameTime time) { /* simulation & input */ }

    public void Draw(GameTime time)
    {
        _ctx.Renderer.Clear(Color.CornflowerBlue);
        _ctx.Sprites.Begin();
        _ctx.Sprites.Draw(_tex, null, new Rectangle(100, 100, _tex.Width, _tex.Height), Color.White);
        _ctx.Sprites.End();
    }
}

public static class Program
{
    public static void Main()
    {
        var host = new SdlHost();
        host.Run(new BasicGame());
    }
}
```
## Shaders (build and runtime lookup)
- Engine HLSL sources live under src/Brine2D.SDL/Content/Shaders (e.g., Sprite.hlsl, Resolve.hlsl).
- During build, Brine2D.SDL compiles HLSL with dxc into:
  - DXIL: SpriteVS.dxil / SpritePS.dxil, ResolveVS.dxil / ResolvePS.dxil
  - SPIR-V: SpriteVS.spv / SpritePS.spv, ResolveVS.spv / ResolvePS.spv
- Compiled blobs are:
  - Embedded into Brine2D.SDL as defaults, and
  - Copied next to the app under Content/Shaders

Runtime lookup order for each required pair (VS/PS):
1) External files in Content/Shaders
2) External files in Assets/Shaders (also supports lowercase content/shaders and assets/shaders)
3) Embedded defaults in the Brine2D.SDL assembly

Custom shaders
- Drop user-provided binaries into either:
  - Content/Shaders, or
  - Assets/Shaders
- Filenames must match the engine’s expectation:
  - SpriteVS.ext and SpritePS.ext
  - ResolveVS.ext and ResolvePS.ext
  - Where ext is one of: .dxil (D3D12), .spv (Vulkan), .msl/.metallib (Metal)
- External files override embedded defaults without rebuilding the DLL.

Note: The build currently produces DXIL and SPIR-V. For Metal you can provide .metallib files in the external shader folders.

## Content and layout
The default host adds two file providers rooted at:
- Content/
- Assets/

Typical layout:

```
Content/
  Shaders/
    SpriteVS.dxil   SpritePS.dxil
    SpriteVS.spv    SpritePS.spv
    SpriteVS.msl    SpritePS.msl
    ResolveVS.*     ResolvePS.*
Assets/
  images/
    player.png
```

The renderer picks shaders based on the active GPU shader format:
- D3D12: .dxil
- Vulkan: .spv
- Metal: .msl or .metallib
If a matching shader is missing, startup will throw with the expected file paths.

## Input system (actions & axes)
- Digital actions and analog axes
- Action sets and explicit enable flags
- Per-action repeat (initial delay + interval) and hold duration tracking
- Axis combination policies: MaxAbs, SumClamped, FirstNonZero
- Smoothing via alpha (0..1) or time-constant tau (seconds)
- JSON load/merge/save and diff export

Example (conceptual):

```csharp
using Brine2D.Core.Input;
using Brine2D.Core.Input.Actions;
using Brine2D.Core.Input.Bindings;

enum Act { Jump }
enum Ax  { MoveX, MoveY }

var map = new InputActions<Act, Ax>();
map.BindAction(Act.Jump, new KeyChordBinding(new KeyChord(Key.Space, KeyboardModifiers.None)));
map.BindAxis(Ax.MoveX, new Composite2KeysAxisBinding(Key.A, Key.D, 1f));
map.BindAxis(Ax.MoveY, new Composite2KeysAxisBinding(Key.W, Key.S, 1f));

// per-frame
map.Update(_ctx.Input, _ctx.Modifiers, _ctx.Mouse, _ctx.Gamepads, time.DeltaSeconds);
if (map.WasPressed(Act.Jump)) { /* jump */ }
var move = map.Get2D(Ax.MoveX, Ax.MoveY);
```

## Samples
- Desktop sample: `samples/Brine2D.Sample.Desktop`
  - Camera follow, simple sprite draws, basic input toggles

Run it from Visual Studio or:

```bash
dotnet run -p samples/Brine2D.Sample.Desktop
```

## Notes
- The host auto-selects a present mode (prefers Mailbox when available).
- If the swapchain is not sRGB, the engine resolves from a linear offscreen target to sRGB via a fullscreen pass.

## Troubleshooting
- Missing shader pair error:
  - Ensure dxc is installed and on PATH (or set DxcExe in Brine2D.SDL.csproj).
  - Confirm compiled files exist under bin/.../Content/Shaders.
  - Verify filenames match SpriteVS/PS.* and ResolveVS/PS.* for your backend.

## Status
Brine2D is early and evolving. Feedback and contributions are welcome.