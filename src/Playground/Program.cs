using Brine2D;

namespace Playground
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var gameHost = new GameHost(new Game());
            gameHost.Run();
        }
    }

    public class Game : Brine2D.Game
    {
        protected override void MousePressed(double x, double y, double button, bool isTouch, double presses)
        {
            Console.WriteLine($"Pressed: {x}, {y}, {button}, {isTouch}, {presses}");
        }

        protected override void MouseReleased(double x, double y, double button, bool isTouch, double presses)
        {
            Console.WriteLine($"Released: {x}, {y}, {button}, {isTouch}, {presses}");
        }
    }
}
