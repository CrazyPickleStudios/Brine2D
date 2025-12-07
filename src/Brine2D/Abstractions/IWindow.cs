using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.Abstractions
{
    public interface IWindow : IDisposable
    {
        string Title { get; set; }
        int Width { get; }
        int Height { get; }
        bool IsVisible { get; }
        void Show();
        void Hide();
    }
}
