using Brine2D.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.Desktop
{
    public sealed class DesktopGameContext : IGameContext
    {
        public DesktopGameContext(IServiceProvider services, IWindow window, IInput input)
        {
            Services = services;
            Window = window;
            Input = input;
        }

        public IServiceProvider Services { get; }
        public IWindow Window { get; }
        public IInput Input { get; }
    }
}
