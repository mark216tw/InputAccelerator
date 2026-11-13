using CommunityToolkit.Mvvm.ComponentModel;
using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;

namespace InputAccelerator.ViewModels;

public sealed partial class TextEntryViewModel : ObservableObject
{
    public TextEntryViewModel(TextEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Id = entry.Id;
        Content = entry.Content;
    }

    public TextEntryViewModel(Guid id, string content)
    {
        Id = id;
        Content = content;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Preview))]
    public partial string Content { get; set; }

    public Guid Id { get; }

    public string Preview => TextPreview.CreateSingleLine(Content);

    public TextEntry ToModel()
    {
        return new TextEntry
        {
            Id = Id,
            Content = Content,
        };
    }
}
