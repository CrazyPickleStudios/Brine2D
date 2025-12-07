namespace Brine2D.Abstractions;

public interface IWindow : IDisposable
{
    int Height { get; }
    bool IsVisible { get; }
    string Title { get; set; }
    int Width { get; }
    void Hide();
    void Show();
}