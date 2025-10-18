namespace Brine2D;

internal sealed record TouchReleasedMessage(object Id, double X, double Y, double DX, double DY, double Pressure)
    : TouchMessage(Id, X, Y, DX, DY, Pressure)
{
}