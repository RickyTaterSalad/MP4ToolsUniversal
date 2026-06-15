using System;
using System.IO;
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
	public OptionsViewModel(AppUserSettings settings)
	{
		ApplyFrom(settings);
		AppSettingsStore.SettingsChanged += OnSettingsChanged;
	}

	private void OnSettingsChanged(object sender, EventArgs e) => ApplyFrom(AppSettingsStore.Current);

	private void ApplyFrom(AppUserSettings loaded)
	{
		loaded ??= new AppUserSettings();
		TempDirectory = loaded.TempDirectory ?? "";
		RetainTemporaryFiles = loaded.RetainTemporaryFiles;
		DefaultOutputDirectory = loaded.DefaultOutputDirectory ?? "";
		OpenOutputFolderOnComplete = loaded.OpenOutputFolderOnComplete;
		DeleteTrimSegmentsAfterTrimAndCombine = loaded.DeleteTrimSegmentsAfterTrimAndCombine ?? true;
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
	private bool _deleteTrimSegmentsAfterTrimAndCombine = true;

	public string SettingsFilePathDisplay => AppSettingsStore.SettingsFilePath;

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
	private void SaveSettings()
	{
		try
		{
			var trimmed = TempDirectory?.Trim() ?? "";
			if (!string.IsNullOrEmpty(trimmed))
				Directory.CreateDirectory(trimmed);

			var outTrimmed = DefaultOutputDirectory?.Trim() ?? "";
			if (!string.IsNullOrEmpty(outTrimmed))
				Directory.CreateDirectory(outTrimmed);

			var s = new AppUserSettings
			{
				TempDirectory = trimmed,
				RetainTemporaryFiles = RetainTemporaryFiles,
				DefaultOutputDirectory = outTrimmed,
				OpenOutputFolderOnComplete = OpenOutputFolderOnComplete,
				DeleteTrimSegmentsAfterTrimAndCombine = DeleteTrimSegmentsAfterTrimAndCombine,
			};
			AppSettingsStore.SaveAndApply(s);
			MP4Tools.Logger.Log($"Settings saved. Temporary files folder: {TempPathHelper.GetTempPath()}");
			MP4Tools.Logger.Log($"Default output folder: {DefaultOutputPathRuntime.Directory}");
			MP4Tools.Logger.Log($"Open output folder on completion: {UiBehaviorSettingsRuntime.OpenOutputFolderOnComplete}");
			MP4Tools.Logger.Log($"Delete trim segments after Trim And Combine: {UiBehaviorSettingsRuntime.DeleteTrimSegmentsAfterTrimAndCombine}");
		}
		catch (Exception ex)
		{
			MP4Tools.Logger.Log($"Could not save settings: {ex.Message}");
		}
	}

	[RelayCommand]
	private void ClearTempFolderPath()
	{
		TempDirectory = "";
	}
}
