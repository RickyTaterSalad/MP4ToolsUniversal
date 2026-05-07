using System;
using System.IO;
using System.Text.Json;

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
}

public static class AppSettingsStore
{
	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

	public static string SettingsFilePath =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MP4Tools", "settings.json");

	public static AppUserSettings LoadOrDefault()
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

	public static void Save(AppUserSettings settings)
	{
		var dir = Path.GetDirectoryName(SettingsFilePath);
		if (!string.IsNullOrEmpty(dir))
			Directory.CreateDirectory(dir);
		File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(settings ?? new AppUserSettings(), JsonOptions));
	}
}
