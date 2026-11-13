namespace InputAccelerator.Core.Models;

public sealed class WindowPlacement
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public WindowPlacement Clone()
    {
        return new WindowPlacement
        {
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
        };
    }
}
