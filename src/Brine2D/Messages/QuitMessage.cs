namespace Brine2D;

internal sealed record QuitMessage(int ExitStatusCode) : Message
{
}