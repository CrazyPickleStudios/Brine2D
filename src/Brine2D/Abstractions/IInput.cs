using Brine2D.Input;

namespace Brine2D.Abstractions;

public interface IInput
{
    bool IsKeyDown(KeyCode key);
    bool IsKeyDown(ScanKey key);
    bool WasKeyPressed(KeyCode key);
    bool WasKeyPressed(ScanKey key);
    bool WasKeyReleased(KeyCode key);
    bool WasKeyReleased(ScanKey key);
}