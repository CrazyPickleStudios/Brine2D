using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.Abstractions
{
    public interface IGameContext
    {
        IServiceProvider Services { get; }
        IWindow Window { get; }
        IInput Input { get; }
    }
}
