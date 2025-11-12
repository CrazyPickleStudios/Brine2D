using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brine2D.SDL.Graphics.Text
{
    internal enum TextScript : byte
    {
        Unknown = 0,
        Latin,
        Cyrillic,
        Greek,
        Arabic,
        Hebrew,
        Devanagari,
        Han,
        Hangul
    }
}
