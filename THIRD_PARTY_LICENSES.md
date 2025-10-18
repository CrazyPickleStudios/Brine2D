Third‑Party Licenses and Full Texts
-----------------------------------------------------------------------
This file contains full license texts for native libraries and third‑party components that Brine2D references or may redistribute. See LICENSE.md for the project license (MIT) and higher‑level attributions.

-----------------------------------------------------------------------
Index (component → license section)
- SDL, SDL_image, SDL_mixer, SDL_ttf → zlib/libpng
- FreeType (used by SDL_ttf) → LicenseRef-FTL (FreeType Project License)
- HarfBuzz, PlutoSVG, PlutoVG, ppy.SDL3-* C# bindings → MIT
-----------------------------------------------------------------------

-----------------------------------------------------------------------
Third‑party SPDX mapping (non‑standard/alias identifiers used in this file)
- LicenseRef-FTL: FreeType Project License (FTL) — https://www.freetype.org/license.html
-----------------------------------------------------------------------

-----------------------------------------------------------------------
zlib/libpng license
SPDX-License-Identifier: Zlib
Used by: SDL, SDL_image, SDL_mixer, SDL_ttf and many SDL satellite libraries
Source references:
- https://github.com/libsdl-org/SDL/blob/main/LICENSE.txt
- https://github.com/libsdl-org/SDL_image/blob/main/LICENSE.txt
- https://github.com/libsdl-org/SDL_mixer/blob/main/LICENSE.txt
- https://github.com/libsdl-org/SDL_ttf/blob/main/LICENSE.txt
-----------------------------------------------------------------------

Copyright (C) the authors
  
This software is provided 'as-is', without any express or implied warranty. In no event will the authors be held liable for any damages arising from the use of this software.

Permission is granted to anyone to use this software for any purpose, including commercial applications, and to alter it and redistribute it freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.

-----------------------------------------------------------------------
SPDX-License-Identifier: LicenseRef-FTL
Used by: FreeType (dependency of SDL_ttf)
Canonical source: https://www.freetype.org/license.html
-----------------------------------------------------------------------

The FreeType Project LICENSE

2006-Jan-27

Copyright 1996-2002, 2006 by David Turner, Robert Wilhelm, and Werner Lemberg

Introduction

The FreeType Project is distributed in several archive packages; some of them may contain, in addition to the FreeType font engine, various tools and contributions which rely on, or relate to, the FreeType Project.

This license applies to all files found in such packages, and which do not fall under their own explicit license. The license affects thus the FreeType font engine, the test programs, documentation and makefiles, at the very least.

This license was inspired by the BSD, Artistic, and IJG (Independent JPEG Group) licenses, which all encourage inclusion and use of free software in commercial and freeware products alike. As a consequence, its main points are that:

  o We don't promise that this software works. However, we will be interested in any kind of bug reports. (`as is' distribution)
  o You can use this software for whatever you want, in parts or full form, without having to pay us. (`royalty-free' usage)
  o You may not pretend that you wrote this software. If you use it, or only parts of it, in a program, you must acknowledge somewhere in your documentation that you have used the FreeType code. (`credits')

We specifically permit and encourage the inclusion of this software, with or without modifications, in commercial products. We disclaim all warranties covering The FreeType Project and assume no liability related to The FreeType Project.

Finally, many people asked us for a preferred form for a credit/disclaimer to use in compliance with this license. We thus encourage you to use the following text:

  "Portions of this software are copyright © <year> The FreeType Project (https://freetype.org). All rights reserved."

Please replace <year> with the value from the FreeType version you actually use.

Legal Terms

0. Definitions

Throughout this license, the terms `package', `FreeType Project', and `FreeType archive' refer to the set of files originally distributed by the authors (David Turner, Robert Wilhelm, and Werner Lemberg) as the `FreeType Project', be they named as alpha, beta or final release.

`You' refers to the licensee, or person using the project, where `using' is a generic term including compiling the project's source code as well as linking it to form a `program' or `executable'. This program is referred to as `a program using the FreeType engine'.

This license applies to all files distributed in the original FreeType Project, including all source code, binaries and documentation, unless otherwise stated in the file in its original, unmodified form as distributed in the original archive. If you are unsure whether or not a particular file is covered by this license, you must contact us to verify this.

The FreeType Project is copyright (C) 1996-2000 by David Turner, Robert Wilhelm, and Werner Lemberg. All rights reserved except as specified below.

1. No Warranty

  THE FREETYPE PROJECT IS PROVIDED `AS IS' WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. IN NO EVENT WILL ANY OF THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY DAMAGES CAUSED BY THE USE OR THE INABILITY TO USE, OF THE FREETYPE PROJECT.

2. Redistribution

  This license grants a worldwide, royalty-free, perpetual and irrevocable right and license to use, execute, perform, compile, display, copy, create derivative works of, distribute and sublicense the FreeType Project (in both source and object code forms) and derivative works thereof for any purpose; and to authorize others to exercise some or all of the rights granted herein, subject to the following conditions:

    o Redistribution of source code must retain this license file (`FTL.TXT') unaltered; any additions, deletions or changes to the original files must be clearly indicated in accompanying documentation. The copyright notices of the unaltered, original files must be preserved in all copies of source files.
    o Redistribution in binary form must provide a disclaimer that states that the software is based in part of the work of the FreeType Team, in the distribution documentation. We also encourage you to put an URL to the FreeType web page in your documentation, though this isn't mandatory.

  These conditions apply to any software derived from or based on the FreeType Project, not just the unmodified files. If you use our work, you must acknowledge us. However, no fee need be paid to us.

3. Advertising

  Neither the FreeType authors and contributors nor you shall use the name of the other for commercial, advertising, or promotional purposes without specific prior written permission.

  We suggest, but do not require, that you use one or more of the following phrases to refer to this software in your documentation or advertising materials: `FreeType Project', `FreeType Engine', `FreeType library', or `FreeType Distribution'.

  As you have not signed this license, you are not required to accept it. However, as the FreeType Project is copyrighted material, only this license, or another one contracted with the authors, grants you the right to use, distribute, and modify it. Therefore, by using, distributing, or modifying the FreeType Project, you indicate that you understand and accept all the terms of this license.

4. Contacts

  There are two mailing lists related to FreeType:

    o freetype@nongnu.org

      Discusses general use and applications of FreeType, as well as future and wanted additions to the library and distribution. If you are looking for support, start in this list if you haven't found anything to help you in the documentation.

    o freetype-devel@nongnu.org

      Discusses bugs, as well as engine internals, design issues, specific licenses, porting, etc.

  Our home page can be found at

  https://www.freetype.org

-----------------------------------------------------------------------
MIT license (third‑party components)
SPDX-License-Identifier: MIT
Used by: HarfBuzz, PlutoSVG, PlutoVG, ppy.SDL3-* C# binding packages, and others
See: LICENSE.md (project MIT text) or upstream projects for identical MIT text
Upstream references:
- HarfBuzz: https://github.com/harfbuzz/harfbuzz/blob/main/LICENSE
- ppy.SDL3-CS packages: https://www.nuget.org/packages/ppy.SDL3-CS/
-----------------------------------------------------------------------

When distributing MIT‑licensed third‑party binaries alongside this project, include the MIT text (same text as project LICENSE.md) with the redistributed artifacts or point to upstream license URLs.

-----------------------------------------------------------------------
Practical redistribution guidance
-----------------------------------------------------------------------

- If you bundle native binaries (DLL/.so/.dylib) for SDL, SDL_image, SDL_mixer, SDL_ttf, FreeType, or any codec/native libraries (libpng, libjpeg/IJG, libwebp, libogg/libvorbis, mpg123, libflac, etc.), include the full license text for each redistributed native library in your release artifacts.
- If you rely on NuGet package consumers to restore packages at build/run time and do not bundle native binaries in your release artifacts, linking to upstream license pages (as documented in LICENSE.md and this file) is generally sufficient.
- Keep package IDs (no versions) in LICENSE.md and recorded versions in the project file (src/Brine2D/Brine2D.csproj) to avoid frequent license file edits.
- For any additional native libraries you choose to redistribute, add their full license texts to this file or to a release‑level THIRD_PARTY_LICENSES included with the distributed archive/installer.

-----------------------------------------------------------------------
References
-----------------------------------------------------------------------
- SDL: https://github.com/libsdl-org/SDL
- SDL_image: https://github.com/libsdl-org/SDL_image
- SDL_mixer: https://github.com/libsdl-org/SDL_mixer
- SDL_ttf: https://github.com/libsdl-org/SDL_ttf
- FreeType: https://www.freetype.org/
- HarfBuzz: https://github.com/harfbuzz/harfbuzz
- ppy.SDL3-CS packages: https://www.nuget.org/packages/ppy.SDL3-CS/
-----------------------------------------------------------------------