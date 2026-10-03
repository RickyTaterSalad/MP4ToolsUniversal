using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MP4Tools.Services;
using MP4ToolsLib;

namespace MP4Tools.ViewModels;

public partial class OptionsViewModel : ViewModelBase
{
	private bool _suppressAutoSave;
	private CancellationTokenSource _persistCts;

	public OptionsViewModel(AppUserSettings settings)
	{
		ApplyFrom(settings);
		AppSettingsStore.SettingsChanged += OnSettingsChanged;
	}

	private void OnSettingsChanged(object sender, EventArgs e) => ApplyFrom(AppSettingsStore.Current);

	private void ApplyFrom(AppUserSettings loaded)
	{
		_suppressAutoSave = true;
		try
		{
			loaded ??= new AppUserSettings();
			TempDirectory = loaded.TempDirectory ?? "";
			RetainTemporaryFiles = loaded.RetainTemporaryFiles;
			DefaultOutputDirectory = loaded.DefaultOutputDirectory ?? "";
			OpenOutputFolderOnComplete = loaded.OpenOutputFolderOnComplete;
			WarnOnInsufficientDiskSpace = loaded.WarnOnInsufficientDiskSpace;
			DeleteTrimSegmentsAfterTrimAndCombine = loaded.DeleteTrimSegmentsAfterTrimAndCombine ?? true;
			UseResolveSafeEncoding = loaded.UseResolveSafeEncoding;
			BaseballLoggerServerUrl = loaded.BaseballLoggerServerUrl ?? "";
			BaseballLoggerApiKey = loaded.BaseballLoggerApiKey ?? "";
			OpenRouterApiKey = loaded.OpenRouterApiKey ?? "";
			OpenRouterModel = loaded.OpenRouterModel ?? "";
			var lrfInterval = loaded.LrfInningSampleIntervalSeconds;
			if (double.IsNaN(lrfInterval) || double.IsInfinity(lrfInterval) || lrfInterval <= 0)
				lrfInterval = 3;
			LrfInningSampleIntervalSeconds = Math.Clamp(lrfInterval, 0.5, 30);
		}
		finally
		{
			_suppressAutoSave = false;
		}
	}

	[ObservableProperty]
	private string _tempDirectory = "";

	[ObservableProperty]
	private string _defaultOutputDirectory = "";

	[ObservableProperty]
	private bool _retainTemporaryFiles;

	[ObservableProperty]
	private bool _openOutputFolderOnComplete;

	[ObservableProperty]
	private bool _warnOnInsufficientDiskSpace = true;

	[ObservableProperty]
	private bool _deleteTrimSegmentsAfterTrimAndCombine = true;

	[ObservableProperty]
	private bool _useResolveSafeEncoding = true;

	[ObservableProperty]
	private string _baseballLoggerServerUrl = "";

	[ObservableProperty]
	private string _baseballLoggerApiKey = "";

	[ObservableProperty]
	private string _openRouterApiKey = "";

	[ObservableProperty]
	private string _openRouterModel = "";

	[ObservableProperty]
	private double _lrfInningSampleIntervalSeconds = 3;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ApiKeyPasswordChar))]
	private bool _isApiKeyVisible;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(OpenRouterApiKeyPasswordChar))]
	private bool _isOpenRouterApiKeyVisible;

	/// <summary>Mask character when hidden; null char clears password masking.</summary>
	public char ApiKeyPasswordChar => IsApiKeyVisible ? '\0' : '*';

	public char OpenRouterApiKeyPasswordChar => IsOpenRouterApiKeyVisible ? '\0' : '*';

	public string SettingsFilePathDisplay => AppSettingsStore.SettingsFilePath;

	partial void OnTempDirectoryChanged(string value) => SchedulePersist();
	partial void OnDefaultOutputDirectoryChanged(string value) => SchedulePersist();
	partial void OnRetainTemporaryFilesChanged(bool value) => PersistNow();
	partial void OnOpenOutputFolderOnCompleteChanged(bool value) => PersistNow();
	partial void OnWarnOnInsufficientDiskSpaceChanged(bool value) => PersistNow();
	partial void OnDeleteTrimSegmentsAfterTrimAndCombineChanged(bool value) => PersistNow();
	partial void OnUseResolveSafeEncodingChanged(bool value) => PersistNow();
	partial void OnBaseballLoggerServerUrlChanged(string value) => SchedulePersist();
	partial void OnBaseballLoggerApiKeyChanged(string value) => SchedulePersist();
	partial void OnOpenRouterApiKeyChanged(string value) => SchedulePersist();
	partial void OnOpenRouterModelChanged(string value) => SchedulePersist();
	partial void OnLrfInningSampleIntervalSecondsChanged(double value) => PersistNow();

	private void PersistNow()
	{
		_persistCts?.Cancel();
		_persistCts?.Dispose();
		_persistCts = null;
		PersistSettings();
	}

	private async void SchedulePersist()
	{
		if (_suppressAutoSave)
			return;

		_persistCts?.Cancel();
		_persistCts?.Dispose();
		var cts = new CancellationTokenSource();
		_persistCts = cts;
		try
		{
			await Task.Delay(400, cts.Token).ConfigureAwait(true);
			PersistSettings();
		}
		catch (OperationCanceledException)
		{
			// superseded by a newer change
		}
	}

	private void PersistSettings()
	{
		if (_suppressAutoSave)
			return;

		try
		{
			var trimmed = TempDirectory?.Trim() ?? "";
			if (!string.IsNullOrEmpty(trimmed))
				Directory.CreateDirectory(trimmed);

			var outTrimmed = DefaultOutputDirectory?.Trim() ?? "";
			if (!string.IsNullOrEmpty(outTrimmed))
				Directory.CreateDirectory(outTrimmed);

			var lrfInterval = LrfInningSampleIntervalSeconds;
			if (double.IsNaN(lrfInterval) || double.IsInfinity(lrfInterval) || lrfInterval <= 0)
				lrfInterval = 3;
			lrfInterval = Math.Clamp(lrfInterval, 0.5, 30);

			var s = new AppUserSettings
			{
				TempDirectory = trimmed,
				RetainTemporaryFiles = RetainTemporaryFiles,
				DefaultOutputDirectory = outTrimmed,
				OpenOutputFolderOnComplete = OpenOutputFolderOnComplete,
				WarnOnInsufficientDiskSpace = WarnOnInsufficientDiskSpace,
				DeleteTrimSegmentsAfterTrimAndCombine = DeleteTrimSegmentsAfterTrimAndCombine,
				UseResolveSafeEncoding = UseResolveSafeEncoding,
				BaseballLoggerServerUrl = BaseballLoggerServerUrl?.Trim() ?? "",
				BaseballLoggerApiKey = BaseballLoggerApiKey?.Trim() ?? "",
				OpenRouterApiKey = OpenRouterApiKey?.Trim() ?? "",
				OpenRouterModel = OpenRouterModel?.Trim() ?? "",
				LrfInningSampleIntervalSeconds = lrfInterval,
			};
			AppSettingsStore.SaveAndApply(s);
			MP4Tools.Logger.Log($"Settings saved. Temporary files folder: {TempPathHelper.GetTempPath()}");
			MP4Tools.Logger.Log($"Default output folder: {DefaultOutputPathRuntime.Directory}");
			MP4Tools.Logger.Log($"Open output folder on completion: {UiBehaviorSettingsRuntime.OpenOutputFolderOnComplete}");
			MP4Tools.Logger.Log($"Warn on insufficient disk space: {UiBehaviorSettingsRuntime.WarnOnInsufficientDiskSpace}");
			MP4Tools.Logger.Log($"Delete trim segments after Trim And Combine: {UiBehaviorSettingsRuntime.DeleteTrimSegmentsAfterTrimAndCombine}");
			MP4Tools.Logger.Log($"Resolve-safe encoding: {EncodingSettingsRuntime.Current.UseResolveSafeEncoding}");
			MP4Tools.Logger.Log(
				$"LRF inning sample interval: {UiBehaviorSettingsRuntime.LrfInningSampleIntervalSeconds:0.###}s");
			MP4Tools.Logger.Log(
				$"Baseball Logger server: {(string.IsNullOrWhiteSpace(BaseballLoggerSettingsRuntime.ServerUrl) ? "(not set)" : BaseballLoggerSettingsRuntime.ServerUrl)}");
			MP4Tools.Logger.Log(
				$"Baseball Logger API key: {(string.IsNullOrWhiteSpace(BaseballLoggerSettingsRuntime.ApiKey) ? "(not set)" : "(saved)")}");
			MP4Tools.Logger.Log(
				$"OpenRouter API key: {(string.IsNullOrWhiteSpace(OpenRouterSettingsRuntime.ApiKey) ? "(not set)" : "(saved)")}");
			MP4Tools.Logger.Log($"OpenRouter model: {OpenRouterSettingsRuntime.Model}");
		}
		catch (Exception ex)
		{
			MP4Tools.Logger.Log($"Could not save settings: {ex.Message}");
		}
	}

	[RelayCommand]
	private async Task BrowseDefaultOutputFolderAsync()
	{
		if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
			return;

		var result = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = "Default folder for combine output and trim exports",
			AllowMultiple = false,
		}).ConfigureAwait(true);

		if (result.Count == 0)
			return;

		foreach (var folder in result)
		{
			var path = folder.TryGetLocalPath();
			if (!string.IsNullOrWhiteSpace(path))
			{
				DefaultOutputDirectory = path;
				break;
			}
		}
	}

	[RelayCommand]
	private void ClearDefaultOutputFolderPath()
	{
		DefaultOutputDirectory = "";
	}

	[RelayCommand]
	private async Task BrowseTempFolderAsync()
	{
		if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
			return;

		var result = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = "Folder for temporary media files",
			AllowMultiple = false,
		}).ConfigureAwait(true);

		if (result.Count == 0)
			return;

		foreach (var folder in result)
		{
			var path = folder.TryGetLocalPath();
			if (!string.IsNullOrWhiteSpace(path))
			{
				TempDirectory = path;
				break;
			}
		}
	}

	[RelayCommand]
	private void ToggleApiKeyVisibility()
	{
		IsApiKeyVisible = !IsApiKeyVisible;
	}

	[RelayCommand]
	private void ToggleOpenRouterApiKeyVisibility()
	{
		IsOpenRouterApiKeyVisible = !IsOpenRouterApiKeyVisible;
	}

	[RelayCommand]
	private void ClearTempFolderPath()
	{
		TempDirectory = "";
	}
}
