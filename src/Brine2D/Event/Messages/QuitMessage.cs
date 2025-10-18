namespace Brine2D.Event.Messages;

internal sealed record QuitMessage(int ExitStatusCode) : Message
{
}