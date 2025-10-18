using Brine2D.Event.Messages;

namespace Brine2D.Event.Messages.Window;

internal sealed record ResizeMessage(double W, double H) : Message
{
}