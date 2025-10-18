namespace Brine2D.Event.Messages.Touch;

internal sealed record TouchMovedMessage(object Id, double X, double Y, double DX, double DY, double Pressure)
    : TouchMessage(Id, X, Y, DX, DY, Pressure)
{
}