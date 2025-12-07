using Brine2D.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.Abstractions
{
    public interface IInput
    {
        bool IsKeyDown(KeyCode key);
        bool WasKeyPressed(KeyCode key);
        bool WasKeyReleased(KeyCode key);
    }
}
