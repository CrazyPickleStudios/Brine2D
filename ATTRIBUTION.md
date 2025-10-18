SPDX-License-Identifier: MIT

Third‑party SPDX identifiers referenced in this repository:
- LÖVE (Love2D) / zlib/libpng: SPDX: Zlib
- FreeType Project License (FTL): SPDX: FTL
- MIT‑licensed third‑party components (ppy.SDL3-*, HarfBuzz, PlutoSVG, PlutoVG): SPDX: MIT

Attribution for Brine2D
-----------------------------------------------------------------------
Brine2D is a C# port and reimplementation derived from the LÖVE (Love2D)
game framework. The public API surface, naming, and many design ideas are
derived from LÖVE. The runtime and implementation in this repository are
written in C# for .NET and distributed under the MIT license (see LICENSE.md).

This file is the canonical, project‑level notice that Brine2D is a derivative
work of LÖVE. Treat the src/Brine2D/ source tree as the primary area where
LÖVE‑inspired or ported material may appear.

-----------------------------------------------------------------------
Licenses & attribution pointers
-----------------------------------------------------------------------
- Project license: MIT — see LICENSE.md.
- LÖVE (Love2D) upstream: https://love2d.org/ — zlib/libpng license (see THIRD_PARTY_LICENSES.md).
- Full third‑party native library license texts and detailed attributions: THIRD_PARTY_LICENSES.md.
- Exact NuGet package versions are recorded in src/Brine2D/Brine2D.csproj.

-----------------------------------------------------------------------
Porting and contributor guidance
-----------------------------------------------------------------------
- This repository is a port/derivative: the public API and many implementation
  patterns are adapted from LÖVE. This repo‑level notice covers those
  adaptations without requiring a per‑file attribution for every source file.
- When adding verbatim or large copied blocks from upstream LÖVE sources
  (beyond short snippets or trivial examples), include a brief file‑level
  comment at the top of that file pointing to the original LÖVE source (URL)
  and describing the nature of the copy/adaptation. This practice improves
  provenance and assists future maintainers.
- For routine porting work and small adaptations, updating this ATTRIBUTION.md
  and using clear commit messages is sufficient.

Suggested concise file‑level comment (use only when adding verbatim/large copied code):
  // Ported/adapted from LÖVE: <URL> — adapted for C#/.NET

-----------------------------------------------------------------------
Third‑party components referenced
-----------------------------------------------------------------------
- LÖVE (Love2D) — https://love2d.org/ — zlib/libpng (see THIRD_PARTY_LICENSES.md)
- ppy SDL C# bindings (NuGet): ppy.SDL3-CS, ppy.SDL3_image-CS, ppy.SDL3_mixer-CS, ppy.SDL3_ttf-CS — MIT
- Native SDL family and satellites: SDL, SDL_image, SDL_mixer, SDL_ttf — zlib/libpng
- SDL_ttf dependencies: FreeType (FTL), HarfBuzz (MIT), PlutoSVG (MIT), PlutoVG (MIT)
- Potential codec/native libs (if redistributed): libpng, libjpeg/IJG, libwebp, libogg/libvorbis, mpg123, libflac — include licenses if redistributing.

-----------------------------------------------------------------------
Redistribution notes
-----------------------------------------------------------------------
- If you redistribute native binaries (DLL/.so/.dylib) in release artifacts,
  include the full license text(s) for each redistributed native library with
  the distribution. The canonical texts are provided in THIRD_PARTY_LICENSES.md;
  include that file in release artifacts or include the specific upstream
  license files next to the redistributed native libraries.
- If you rely on consumers restoring NuGet packages at build time and do not
  bundle native binaries, linking to upstream license pages is generally
  sufficient. Verify the contents of any NuGet packages you redistribute.

-----------------------------------------------------------------------
Contact & contributions
-----------------------------------------------------------------------
- Contributions are credited via commit history and contributor listings. For
  attribution or licensing questions, open an issue or contact the repository
  maintainers.