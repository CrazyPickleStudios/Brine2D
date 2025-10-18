SPDX-License-Identifier: MIT

MIT License
-----------------------------------------------------------------------
Copyright (c) 2025 CrazyPickle Studios

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

-----------------------------------------------------------------------
Third‑party attributions and additional license texts
-----------------------------------------------------------------------

This repository includes or references third‑party software and native libraries.
Full texts and additional third‑party license attributions are collected in
THIRD_PARTY_LICENSES.md. See ATTRIBUTION.md for higher‑level attribution notes
and provenance information.

Notable third‑party components referenced by this project:
- LÖVE (Love2D) — zlib/libpng license (used as API/design inspiration; referenced/ported material). See THIRD_PARTY_LICENSES.md for the zlib/libpng text and details.
- ppy SDL C# bindings:
  - ppy.SDL3-CS
  - ppy.SDL3_image-CS
  - ppy.SDL3_mixer-CS
  - ppy.SDL3_ttf-CS
  (These C# packages are MIT‑licensed. Exact package versions are recorded in src/Brine2D/Brine2D.csproj.)
- Native SDL family and satellites:
  - SDL, SDL_image, SDL_mixer, SDL_ttf — zlib/libpng (see THIRD_PARTY_LICENSES.md)
- SDL_ttf dependencies:
  - FreeType — FreeType Project License (FTL)
  - HarfBuzz — MIT
  - PlutoSVG — MIT
  - PlutoVG — MIT

If you redistribute any native binaries (for example, SDL, SDL_image, SDL_mixer, SDL_ttf, FreeType, or codec libraries such as libpng, libjpeg, libwebp, libogg/libvorbis, mpg123, libflac), you MUST include the full license text(s) for those redistributed native libraries with your distribution. The canonical texts for these dependencies are provided in THIRD_PARTY_LICENSES.md; include them in release artifacts when bundling native DLLs/.so/.dylib files.

Note on NuGet packages
- This repository records package IDs in the project file (src/Brine2D/Brine2D.csproj). Rely on the csproj for exact versions to avoid per‑version edits in LICENSE.md.
- Some NuGet packages (notably several ppy.SDL3-* packages) may include native binaries in the package payload. If your release artifacts include those native binaries (i.e., you bundle them into a ZIP, installer, or other packaged release), ensure you include the corresponding full license texts and notices in the release distribution.

Where to find more information
- ATTRIBUTION.md — high‑level attribution and notes on what was ported or adapted.
- THIRD_PARTY_LICENSES.md — full third‑party license texts and detailed attributions.
- src/Brine2D/Brine2D.csproj — exact NuGet package versions referenced by this project.

-----------------------------------------------------------------------