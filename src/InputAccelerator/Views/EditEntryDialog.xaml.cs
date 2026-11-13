using InputAccelerator.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InputAccelerator.Views;

public sealed partial class EditEntryDialog : ContentDialog
{
    public EditEntryDialog(string title, string initialText)
    {
        InitializeComponent();
        Title = title;
        PrimaryButtonText = Strings.Get("EditEntryDialog.PrimaryButtonText", "儲存");
        CloseButtonText = Strings.Get("EditEntryDialog.CloseButtonText", "取消");
        ContentBox.Text = initialText;
        Loaded += OnLoaded;
    }

    public string EntryText => ContentBox.Text;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ContentBox.Focus(FocusState.Programmatic);
        ContentBox.Select(ContentBox.Text.Length, 0);
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(ContentBox.Text))
        {
            args.Cancel = true;
            ValidationMessage.Visibility = Visibility.Visible;
            ContentBox.Focus(FocusState.Programmatic);
        }
    }

    private void OnContentChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ContentBox.Text))
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
        }
    }
}
