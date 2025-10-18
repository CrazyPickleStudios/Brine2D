🎮 Brine2D: A Convention-Free Game Engine for C#
-----------------------------------------------------------------------

Brine2D is a lightweight, convention-free game engine for C# developers inspired by the elegance and simplicity of the Love2D public surface API. It brings the expressive power of Love2D to the .NET ecosystem, offering a familiar structure with modern C# ergonomics.

![Design Philosophy: Instance-based, convention-free, Love2D-inspired](https://img.shields.io/badge/design%20philosophy-instance--based%2C%20convention--free%2C%20Love2D--inspired-blue)

-----------------------------------------------------------------------
✨ Key Features
-----------------------------------------------------------------------

- **Love2D-inspired API**: Most function names and module structures mirror Love2D for intuitive onboarding.
- **Convention-free architecture**: No rigid project scaffolding or boilerplate—just plug in and play.
- **C# idiomatic design**: Fully ported to C#, embracing object-oriented patterns and .NET conventions.
- **Modular Surface API**: Love2D modules are reimagined as `*Module` classes for clarity and extensibility.
- **Instance-based access**: Modules are accessed through the `Game` instance, not statically.

-----------------------------------------------------------------------
🚀 Getting Started
-----------------------------------------------------------------------

Here's a minimal example to get you up and running:

```csharp
internal class Program
{
    private static void Main(string[] args)
    {
        var gameHost = new GameHost(new Game());
        gameHost.Run();
    }
}

public class Game : Brine2D.Game
{
    protected override void MousePressed(double x, double y, double button, bool isTouch, double presses)
    {
        Console.WriteLine($"Pressed: {x}, {y}, {button}, {isTouch}, {presses}");
    }

    protected override void MouseReleased(double x, double y, double button, bool isTouch, double presses)
    {
        Console.WriteLine($"Released: {x}, {y}, {button}, {isTouch}, {presses}");
    }
}
```

-----------------------------------------------------------------------
🧩 Module Mapping
-----------------------------------------------------------------------

Brine2D preserves the spirit of Love2D modules with a simple naming convention:

| Love2D Module   | Brine2D Equivalent |
|-----------------|--------------------|
| `love.graphics` | `GraphicsModule`   |
| `love.audio`    | `AudioModule`      |
| `love.input`    | `InputModule`      |
| `love.timer`    | `TimerModule`      |
| `love.math`     | `MathModule`       |

Each module is accessed via the `Game` instance (e.g., `game.Graphics.Draw(...)`) and exposes familiar methods adapted for C# idioms and type safety.

-----------------------------------------------------------------------
📘 Module Access Philosophy
-----------------------------------------------------------------------

Brine2D preserves the spirit of LÖVE modules with a simple naming convention. Modules are exposed as instance properties on the Game class (for example, `game.Graphics`, `game.Keyboard`). Internally, Module instances register themselves with a static registry when they are constructed (see `Module` base class), which enables LÖVE-like module lookup behavior while keeping user code instance-based and idiomatic for .NET.

This design preserves the global feel of Love2D while maintaining C# best practices:

- ✅ Scoped access via `game.Graphics`, `game.Keyboard`, etc.
- ✅ Thread safety and lifecycle control
- ✅ Clean separation between user-facing API and internal engine state

We intentionally do not expose static access to modules (e.g., no `Module.GetInstance<T>()` in user code) to preserve encapsulation and prevent misuse.

-----------------------------------------------------------------------
🛠 Architecture Notes
-----------------------------------------------------------------------

- **Game lifecycle**: Override methods like `Load()`, `Update(dt)`, `Draw()`, `MousePressed(...)`, etc.
- **Event-driven input**: Mouse and keyboard events are surfaced through overridable methods.
- **No magic**: You control the flow—no hidden conventions or auto-wiring.

-----------------------------------------------------------------------
📦 Installation
-----------------------------------------------------------------------

Brine2D is distributed as a NuGet package (coming soon). For now, clone the repo and reference the project directly.

```bash
git clone https://github.com/CrazyPickleStudios/Brine2D
```

-----------------------------------------------------------------------
🧪 Testing & Debugging
-----------------------------------------------------------------------

- Console output is fully supported for quick debugging.
- Use `GraphicsModule.DrawText(...)` and other primitives for visual debugging.
- Input events like `MousePressed`, `KeyReleased`, etc. can be logged or traced directly from your `Game` subclass.
- Brine2D favors explicit control—so you can easily isolate and test lifecycle methods like `Load()`, `Update(dt)`, and `Draw()`.

-----------------------------------------------------------------------
📚 Documentation
-----------------------------------------------------------------------

Full API documentation is in progress. For now, refer to the [Love2D wiki](https://love2d.org/wiki/Main_Page) for conceptual guidance—most functions are directly portable.

-----------------------------------------------------------------------
❤️ Philosophy
-----------------------------------------------------------------------

Brine2D is built for developers who value clarity, control, and creative freedom. It’s ideal for prototyping, game jams, and educational projects. The engine favors explicit design over convention, and instance-based access over global state—while staying true to the spirit of Love2D.

-----------------------------------------------------------------------
📄 License & attribution
-----------------------------------------------------------------------

See LICENSE.md and THIRD_PARTY_LICENSES.md for the project license and third‑party license texts and attributions. Exact NuGet package versions are recorded in src/Brine2D/Brine2D.csproj.

Brine2D is an independent project and is not affiliated with or endorsed by the LÖVE project.

-----------------------------------------------------------------------
🔧 Native dependencies & redistribution
-----------------------------------------------------------------------

Brine2D depends on native SDL libraries at runtime (provided by the ppy.SDL3-* NuGet packages). Two simple rules cover packaging and redistribution:

- If you bundle any native binaries (DLL/.so/.dylib) in a release artifact, include the full upstream license text(s) for each redistributed native library alongside those binaries (for example, in THIRD_PARTY_LICENSES.md or as separate upstream license files).
- If you do not bundle native binaries and instead rely on consumers restoring NuGet packages at build/run time, document the native runtime requirement in your release notes and point to THIRD_PARTY_LICENSES.md and the upstream project pages.

Quick release checklist
- Inspect NuGet package contents for native runtimes:
  - dotnet list package --include-transitive
  - Check your local package folder (e.g., ~/.nuget/packages/ppy.sdl3-cs/<version>/runtimes) for native files you may redistribute.
- If bundling:
  - Include the actual license texts for each bundled native library in the release artifact.
  - Supply a short manifest (text file) listing which native binaries are included and the corresponding license filenames.
- If not bundling:
  - Add a note to release notes / download page describing required native runtimes and linking to THIRD_PARTY_LICENSES.md.

See THIRD_PARTY_LICENSES.md for canonical license texts and upstream links.

-----------------------------------------------------------------------
📦 Third‑party packages & versions
-----------------------------------------------------------------------

This repository references the ppy.SDL3-* NuGet packages for SDL bindings. Package IDs are listed in src/Brine2D/Brine2D.csproj; use that file to find exact versions. To inspect packages locally use __Manage NuGet Packages for Solution__ or run:

```bash
dotnet list package --include-transitive
```

-----------------------------------------------------------------------
⚙️ Running locally (quick checklist)
-----------------------------------------------------------------------

- Prerequisites
  - Install the .NET 9 SDK.
  - Native SDL runtime libraries must be available at runtime (see "Native runtime notes" below).

- Clone & restore

    ```bash
    git clone https://github.com/CrazyPickleStudios/Brine2D
    cd Brine2D
    dotnet restore
    ```

- Build

    ```bash
    dotnet build -c Debug
    # or for release:
    dotnet build -c Release
    ```

- Run the sample GameHost example
  - From the sample project folder:

    ```bash
    dotnet run
    ```
  - Visual Studio: open the solution, __Set as Startup Project__ on the sample project, then __Start Debugging__ (F5) or __Start Without Debugging__ (Ctrl+F5).

- Verify packages

    ```bash
    dotnet list package --include-transitive
    ```

- Native runtime notes
  - Windows: ensure SDL3/*.dll are next to the executable or on PATH.
  - macOS (Homebrew): brew install sdl3 sdl3_image sdl3_mixer sdl3_ttf freetype
  - Debian/Ubuntu: apt install libsdl3-2.0-0 libsdl3-image-2.0-0 libsdl3-mixer-2.0-0 libsdl3-ttf-2.0-0 libfreetype6
  - If startup errors mention missing SDL/font libraries, install or copy the native libraries into the app folder.

- Publish

    ```bash
    dotnet publish -c Release -r <RID> --self-contained false
    # Example for Windows x64:
    # dotnet publish -c Release -r win-x64 --self-contained false
    ```

- Troubleshooting
  - Check console output for missing native library messages.
  - Confirm exact NuGet package versions in src/Brine2D/Brine2D.csproj if reproducing a bug.
  - Consult the ppy.SDL3-* package documentation for platform-specific native library details.

-----------------------------------------------------------------------
🛠 Contributing & issues
-----------------------------------------------------------------------

- Open issues or PRs on the repository for bugs, feature requests, or licensing questions.
- When submitting changes that port or quote LÖVE content, add a short file‑level comment pointing to the original source and update ATTRIBUTION.md if needed.