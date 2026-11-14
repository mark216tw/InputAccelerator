using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InputAccelerator.Core.Models;
using InputAccelerator.Services;
using Microsoft.Extensions.Logging;

namespace InputAccelerator.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly AppearanceService _appearance;
    private readonly ILogger<SettingsViewModel> _logger;
    private bool _isLoading;
    private InputMode _savedInputMode;
    private AppTheme _savedTheme;
    private int _savedBackgroundIndex;
    private int _savedAutoCollapseSeconds;

    public SettingsViewModel(
        SettingsService settings,
        AppearanceService appearance,
        ILogger<SettingsViewModel> logger)
    {
        _settings = settings;
        _appearance = appearance;
        _logger = logger;
        BackgroundOptions = new ObservableCollection<FloatingBackgroundOption>(FloatingBackgroundOption.CreateAll());
    }

    [ObservableProperty]
    public partial int InputModeIndex { get; set; }

    [ObservableProperty]
    public partial int ThemeIndex { get; set; }

    [ObservableProperty]
    public partial FloatingBackgroundOption? SelectedBackgroundOption { get; set; }

    [ObservableProperty]
    public partial int FloatingAutoCollapseIndex { get; set; }

    public ObservableCollection<FloatingBackgroundOption> BackgroundOptions { get; }

    public bool HasUnsavedChanges =>
        GetInputMode() != _savedInputMode ||
        GetTheme() != _savedTheme ||
        GetBackgroundIndex() != _savedBackgroundIndex ||
        GetAutoCollapseSeconds() != _savedAutoCollapseSeconds;

    public void Reload()
    {
        LoadFromSettings(_settings.Current);
    }

    public void LoadFromSettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _isLoading = true;
        try
        {
            InputModeIndex = settings.InputMode is InputMode.ClipboardPaste ? 1 : 0;
            ThemeIndex = settings.Theme switch
            {
                AppTheme.Light => 1,
                AppTheme.Dark => 2,
                _ => 0,
            };
            SelectedBackgroundOption = BackgroundOptions.FirstOrDefault(
                option => option.Index == settings.FloatingBackgroundIndex) ?? BackgroundOptions[0];
            FloatingAutoCollapseIndex = FloatingAutoCollapsePresets.GetIndex(settings.FloatingAutoCollapseSeconds);

            _savedInputMode = settings.InputMode;
            _savedTheme = settings.Theme;
            _savedBackgroundIndex = settings.FloatingBackgroundIndex;
            _savedAutoCollapseSeconds = settings.FloatingAutoCollapseSeconds;
        }
        finally
        {
            _isLoading = false;
        }

        OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        if (!HasUnsavedChanges)
        {
            return true;
        }

        var inputMode = GetInputMode();
        var theme = GetTheme();
        var backgroundIndex = GetBackgroundIndex();
        var autoCollapseSeconds = GetAutoCollapseSeconds();

        _settings.Current.InputMode = inputMode;
        _settings.Current.Theme = theme;
        _settings.Current.FloatingBackgroundIndex = backgroundIndex;
        _settings.Current.FloatingAutoCollapseSeconds = autoCollapseSeconds;
        _appearance.Apply(theme, backgroundIndex);

        try
        {
            await _settings.SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _settings.Current.InputMode = _savedInputMode;
            _settings.Current.Theme = _savedTheme;
            _settings.Current.FloatingBackgroundIndex = _savedBackgroundIndex;
            _settings.Current.FloatingAutoCollapseSeconds = _savedAutoCollapseSeconds;
            _appearance.Apply(_savedTheme, _savedBackgroundIndex);
            _logger.LogError(ex, "儲存設定失敗。");
            return false;
        }

        _savedInputMode = inputMode;
        _savedTheme = theme;
        _savedBackgroundIndex = backgroundIndex;
        _savedAutoCollapseSeconds = autoCollapseSeconds;
        OnPropertyChanged(nameof(HasUnsavedChanges));
        return true;
    }

    partial void OnInputModeIndexChanged(int value)
    {
        NotifyUnsavedChanges();
    }

    partial void OnThemeIndexChanged(int value)
    {
        NotifyUnsavedChanges();
    }

    partial void OnSelectedBackgroundOptionChanged(FloatingBackgroundOption? value)
    {
        NotifyUnsavedChanges();
    }

    partial void OnFloatingAutoCollapseIndexChanged(int value)
    {
        NotifyUnsavedChanges();
    }

    private void NotifyUnsavedChanges()
    {
        if (!_isLoading)
        {
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    private InputMode GetInputMode()
    {
        return InputModeIndex == 1 ? InputMode.ClipboardPaste : InputMode.SimulatedInput;
    }

    private AppTheme GetTheme()
    {
        return ThemeIndex switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.System,
        };
    }

    private int GetBackgroundIndex()
    {
        return SelectedBackgroundOption?.Index ?? _savedBackgroundIndex;
    }

    private int GetAutoCollapseSeconds()
    {
        return FloatingAutoCollapsePresets.GetSeconds(FloatingAutoCollapseIndex);
    }
}
