namespace Brine2D;

internal sealed record TouchPressedMessage(object Id, double X, double Y, double DX, double DY, double Pressure)
    : TouchMessage(Id, X, Y, DX, DY, Pressure)
{
}