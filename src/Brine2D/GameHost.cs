namespace Brine2D;

// TODO: Does love::Exception actually throw?
public class GameHost
{
    private readonly Game _game;

    public GameHost(Game game)
    {
        _game = game;
    }

    public int Run()
    {
        _game.Boot();
        _game.Init();

        var mainLoop = _game.Run();
        while (true)
        {
            int? result = mainLoop();
            if (result.HasValue)
                return result.Value;
        }
    }
}