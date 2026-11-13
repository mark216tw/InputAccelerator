using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace InputAccelerator.Helpers;

public sealed class WrapPanel : Panel
{
    public static readonly DependencyProperty HorizontalSpacingProperty = DependencyProperty.Register(
        nameof(HorizontalSpacing),
        typeof(double),
        typeof(WrapPanel),
        new PropertyMetadata(0d, OnSpacingChanged));

    public static readonly DependencyProperty VerticalSpacingProperty = DependencyProperty.Register(
        nameof(VerticalSpacing),
        typeof(double),
        typeof(WrapPanel),
        new PropertyMetadata(0d, OnSpacingChanged));

    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var availableWidth = double.IsInfinity(availableSize.Width)
            ? double.PositiveInfinity
            : Math.Max(0, availableSize.Width);
        var horizontalSpacing = Math.Max(0, HorizontalSpacing);
        var verticalSpacing = Math.Max(0, VerticalSpacing);

        double lineWidth = 0;
        double lineHeight = 0;
        double totalHeight = 0;
        double widestLine = 0;

        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var childSize = child.DesiredSize;

            if (lineWidth > 0 && lineWidth + horizontalSpacing + childSize.Width > availableWidth)
            {
                widestLine = Math.Max(widestLine, lineWidth);
                totalHeight += lineHeight + verticalSpacing;
                lineWidth = 0;
                lineHeight = 0;
            }

            if (lineWidth > 0)
            {
                lineWidth += horizontalSpacing;
            }

            lineWidth += childSize.Width;
            lineHeight = Math.Max(lineHeight, childSize.Height);
        }

        widestLine = Math.Max(widestLine, lineWidth);
        totalHeight += lineHeight;

        var desiredWidth = double.IsInfinity(availableWidth) ? widestLine : availableWidth;
        return new Size(desiredWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var availableWidth = double.IsInfinity(finalSize.Width)
            ? double.PositiveInfinity
            : Math.Max(0, finalSize.Width);
        var horizontalSpacing = Math.Max(0, HorizontalSpacing);
        var verticalSpacing = Math.Max(0, VerticalSpacing);

        double x = 0;
        double y = 0;
        double lineHeight = 0;

        foreach (var child in Children)
        {
            var childSize = child.DesiredSize;

            if (x > 0 && x + horizontalSpacing + childSize.Width > availableWidth)
            {
                x = 0;
                y += lineHeight + verticalSpacing;
                lineHeight = 0;
            }

            if (x > 0)
            {
                x += horizontalSpacing;
            }

            child.Arrange(new Rect(x, y, childSize.Width, childSize.Height));
            x += childSize.Width;
            lineHeight = Math.Max(lineHeight, childSize.Height);
        }

        return finalSize;
    }

    private static void OnSpacingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((WrapPanel)sender).InvalidateMeasure();
    }
}
