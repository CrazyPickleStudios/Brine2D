# Prerequisites

Before building with Brine2D, ensure your environment is set up with the required SDKs, tools, and platform dependencies.

## Required
- .NET 10 SDK
- Visual Studio 2026 with .NET development workload
- Git (optional but recommended)
- OS: Windows, macOS, or Linux

Verify your .NET SDK:
~~~console
dotnet --info
~~~

## Recommended
- Latest GPU drivers for your platform
- A modern terminal (Windows Terminal, iTerm2, GNOME Terminal)
- A package manager (winget, Homebrew, apt)

## Platform Dependencies

Brine2D’s desktop runtime uses SDL3 for windowing, input, graphics, and audio. The engine bundles or resolves SDL3 as part of the desktop backend. Some platforms may require runtime libraries:

- Windows: VC++ runtime (installed with Visual Studio). Keep graphics and audio drivers updated.
- macOS: No additional runtime usually required. Ensure Gatekeeper allows app execution.
- Linux: SDL3 may require distribution packages. Install common dependencies:
  - Ubuntu/Debian:
    ~~~console
    sudo apt update
    sudo apt install libsdl3
    ~~~
  - Fedora:
    ~~~console
    sudo dnf install SDL3
    ~~~

Notes:
- Package names can vary by distro and version; consult your distribution’s repositories.
- If using a self-contained publish, these may be bundled automatically.

## Project Setup

Create a new project and add the desktop backend:
~~~console
dotnet new console -n Brine2DGame -f net10.0
cd Brine2DGame
dotnet add package Brine2D.Desktop
~~~

Open in Visual Studio:
- Use __File > Open > Folder__ or __File > Open > Project/Solution__.
- Ensure Target Framework is set to `.NET 10`.

## Troubleshooting

- dotnet not found: reinstall the .NET 10 SDK and restart your shell.
- Build errors about missing SDL libraries: verify platform packages or use self-contained publish.
- Visual Studio workload missing: run the Visual Studio Installer and add the .NET development workload.
- Permission issues on macOS/Linux: ensure executable permissions and correct file paths for assets.

## Next Steps
- Get started: [Getting Started](./getting-started.md)
- Runtime model: [Runtime Overview](./runtime/overview.md)
- Gameplay patterns: [Gameplay Overview](./gameplay/overview.md)