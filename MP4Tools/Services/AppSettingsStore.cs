using System;
using System.IO;
using System.Text.Json;

namespace MP4Tools.Services;

public sealed class AppUserSettings
{
	public string TempDirectory { get; set; } = "";
	public bool RetainTemporaryFiles { get; set; }

	/// <summary>Folder for default Combine paths and Trim session folders. Empty in JSON uses <see cref="DefaultOutputPathRuntime.BuiltinFallbackDirectory"/>.</summary>
	public string DefaultOutputDirectory { get; set; } = "";
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
