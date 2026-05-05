namespace MP4Tools.Services;

/// <summary>Resolved default folder for Combine output filenames and Trim export folders.</summary>
public static class DefaultOutputPathRuntime
{
	public static string BuiltinFallbackDirectory => System.OperatingSystem.IsLinux() ?  "/opt/Encodes" : string.Empty;

	public static string Directory { get; private set; } = BuiltinFallbackDirectory;

	public static void Apply(AppUserSettings settings)
	{
		var raw = settings?.DefaultOutputDirectory?.Trim();
		Directory = string.IsNullOrEmpty(raw) ? BuiltinFallbackDirectory : raw;
	}
}
