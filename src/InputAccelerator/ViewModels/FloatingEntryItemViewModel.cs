using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InputAccelerator.Services;
using Microsoft.UI.Xaml.Media;

namespace InputAccelerator.ViewModels;

public sealed partial class FloatingEntryItemViewModel : ObservableObject, IDisposable
{
    private readonly TextEntryViewModel _entry;

    public FloatingEntryItemViewModel(TextEntryViewModel entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entry = entry;
        Preview = entry.Preview;
        entry.PropertyChanged += OnEntryPropertyChanged;
    }

    public TextEntryViewModel Entry => _entry;

    [ObservableProperty]
    public partial string Preview { get; set; }

    [ObservableProperty]
    public partial Brush? ChipBackground { get; set; }

    [ObservableProperty]
    public partial Brush? ChipBorder { get; set; }

    [ObservableProperty]
    public partial Brush? ChipForeground { get; set; }

    public void ApplyPalette(FloatingPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ChipBackground = palette.ChipBackground;
        ChipBorder = palette.ChipBorder;
        ChipForeground = palette.Foreground;
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TextEntryViewModel.Preview))
        {
            Preview = _entry.Preview;
        }
    }

    public void Dispose()
    {
        _entry.PropertyChanged -= OnEntryPropertyChanged;
    }
}
