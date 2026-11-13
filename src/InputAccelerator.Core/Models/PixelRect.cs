namespace InputAccelerator.Core.Models;

public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;

    public long Area => (long)Math.Max(0, Width) * Math.Max(0, Height);

    public bool Contains(int x, int y)
    {
        return x >= X && x < Right && y >= Y && y < Bottom;
    }

    public PixelRect Intersect(PixelRect other)
    {
        var left = Math.Max(X, other.X);
        var top = Math.Max(Y, other.Y);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);
        return right <= left || bottom <= top
            ? new PixelRect(left, top, 0, 0)
            : new PixelRect(left, top, right - left, bottom - top);
    }
}

public sealed record DisplayAreaInfo(PixelRect Bounds, PixelRect WorkArea);
