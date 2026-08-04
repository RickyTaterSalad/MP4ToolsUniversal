using System;
using System.IO;
using System.Text.Json;
using MP4ToolsLib;

namespace MP4Tools.Services;

public sealed class AppUserSettings
{
	public string TempDirectory { get; set; } = "";
	public bool RetainTemporaryFiles { get; set; }

	public string DefaultOutputDirectory { get; set; } = "";

	/// <summary>
	/// When enabled, the app opens the output folder in the OS file manager after Combine or Trim completes successfully.
	/// </summary>
	public bool OpenOutputFolderOnComplete { get; set; }

	/// <summary>
	/// When true (default), successful Trim And Combine removes the intermediate segment clips folder under the export directory.
	/// </summary>
	public bool? DeleteTrimSegmentsAfterTrimAndCombine { get; set; }

	/// <summary>
	/// When true (default), trim/combine re-encode to Resolve-safe HEVC MP4. When false, use fast stream-copy (may break DaVinci Resolve).
	/// </summary>
	public bool UseResolveSafeEncoding { get; set; } = true;

	/// <summary>
	/// Baseball Logger Server base URL (e.g. https://logger.example.com). Used when uploading a modified recording
	/// with a bare share token; share URLs already include the host.
	/// </summary>
	public string BaseballLoggerServerUrl { get; set; } = "";

	/// <summary>
	/// Baseball Logger Server API key (<c>apikey</c> header). Optional for share-token revision uploads;
	/// required for box-score image upload.
	/// </summary>
	public string BaseballLoggerApiKey { get; set; } = "";
}

public static class AppSettingsStore
{
	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

	public static event EventHandler SettingsChanged;

	public static AppUserSettings Current { get; private set; } = new();

	public static string SettingsFilePath =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MP4Tools", "settings.json");

	public static AppUserSettings LoadFromDisk()
	{
		try
		{
			var path = SettingsFilePath;
			if (File.Exists(path))
			{
				var json = File.ReadAllText(path);
				var loaded = JsonSerializer.Deserialize<AppUserSettings>(json);
				if (loaded != null)
					return loaded;
			}
		}
		catch
		{
			// ignored — fall back to defaults
		}

		return new AppUserSettings();
	}

	public static AppUserSettings LoadAndApply()
	{
		var settings = LoadFromDisk();
		Apply(settings);
		return settings;
	}

	public static void Apply(AppUserSettings settings)
	{
		Current = settings ?? new AppUserSettings();
		TempPathHelper.ApplyConfiguration(Current.TempDirectory, Current.RetainTemporaryFiles);
		EncodingSettingsRuntime.Apply(Current);
		SettingsChanged?.Invoke(null, EventArgs.Empty);
	}

	public static void Save(AppUserSettings settings)
	{
		var dir = Path.GetDirectoryName(SettingsFilePath);
		if (!string.IsNullOrEmpty(dir))
			Directory.CreateDirectory(dir);
		File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(settings ?? new AppUserSettings(), JsonOptions));
	}

	public static void SaveAndApply(AppUserSettings settings)
	{
		Save(settings);
		Apply(settings);
	}
}
