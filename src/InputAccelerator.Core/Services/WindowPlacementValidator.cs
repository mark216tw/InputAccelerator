using InputAccelerator.Core.Models;

namespace InputAccelerator.Core.Services;

public static class WindowPlacementValidator
{
    public static WindowPlacement Normalize(
        WindowPlacement? placement,
        IReadOnlyList<DisplayAreaInfo> displays,
        int minWidth,
        int minHeight,
        int defaultWidth,
        int defaultHeight)
    {
        minWidth = Math.Max(1, minWidth);
        minHeight = Math.Max(1, minHeight);
        defaultWidth = Math.Max(defaultWidth, minWidth);
        defaultHeight = Math.Max(defaultHeight, minHeight);

        if (displays.Count == 0)
        {
            var fallback = new PixelRect(0, 0, Math.Max(defaultWidth, minWidth), Math.Max(defaultHeight, minHeight));
            displays = [new DisplayAreaInfo(fallback, fallback)];
        }

        var primary = displays[0];
        if (placement is null || placement.Width <= 0 || placement.Height <= 0)
        {
            return CreateDefault(primary.WorkArea, minWidth, minHeight, defaultWidth, defaultHeight);
        }

        var display = FindBestDisplay(placement, displays);
        var workArea = display.WorkArea;
        var width = Math.Clamp(placement.Width, minWidth, Math.Max(minWidth, workArea.Width));
        var height = Math.Clamp(placement.Height, minHeight, Math.Max(minHeight, workArea.Height));

        var maxX = Math.Max(workArea.X, workArea.Right - width);
        var maxY = Math.Max(workArea.Y, workArea.Bottom - height);
        var x = Math.Clamp(placement.X, workArea.X, maxX);
        var y = Math.Clamp(placement.Y, workArea.Y, maxY);

        return new WindowPlacement
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
        };
    }

    private static WindowPlacement CreateDefault(
        PixelRect workArea,
        int minWidth,
        int minHeight,
        int defaultWidth,
        int defaultHeight)
    {
        var width = Math.Clamp(defaultWidth, minWidth, Math.Max(minWidth, workArea.Width));
        var height = Math.Clamp(defaultHeight, minHeight, Math.Max(minHeight, workArea.Height));
        return new WindowPlacement
        {
            X = workArea.X + Math.Max(0, (workArea.Width - width) / 2),
            Y = workArea.Y + Math.Max(0, (workArea.Height - height) / 2),
            Width = width,
            Height = height,
        };
    }

    private static DisplayAreaInfo FindBestDisplay(WindowPlacement placement, IReadOnlyList<DisplayAreaInfo> displays)
    {
        var savedRect = new PixelRect(placement.X, placement.Y, Math.Max(1, placement.Width), Math.Max(1, placement.Height));

        DisplayAreaInfo? best = null;
        long bestArea = 0;
        foreach (var display in displays)
        {
            var area = savedRect.Intersect(display.WorkArea).Area;
            if (area > bestArea)
            {
                bestArea = area;
                best = display;
            }
        }

        if (best is not null)
        {
            return best;
        }

        var centerX = savedRect.X + (savedRect.Width / 2);
        var centerY = savedRect.Y + (savedRect.Height / 2);
        var nearest = displays[0];
        var nearestDistance = long.MaxValue;
        foreach (var display in displays)
        {
            var nearestX = Math.Clamp(centerX, display.WorkArea.X, display.WorkArea.Right);
            var nearestY = Math.Clamp(centerY, display.WorkArea.Y, display.WorkArea.Bottom);
            var dx = (long)centerX - nearestX;
            var dy = (long)centerY - nearestY;
            var distance = (dx * dx) + (dy * dy);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = display;
            }
        }

        return nearest;
    }
}
