using System;
using System.IO;

namespace MP4ToolsLib
{
	public static class TempPathHelper
	{
		private static string _configuredDirectory;
		private static bool _retainTemporaryFiles;

		public static void ApplyConfiguration(string customTempDirectory, bool retainTemporaryFiles)
		{
			_retainTemporaryFiles = retainTemporaryFiles;
			_configuredDirectory = string.IsNullOrWhiteSpace(customTempDirectory)
				? null
				: Path.GetFullPath(customTempDirectory.Trim());
		}

		public static string GetTempPath()
		{
			var candidate = _configuredDirectory;
			if (string.IsNullOrEmpty(candidate))
				return Path.GetTempPath();

			try
			{
				if (!Directory.Exists(candidate))
					Directory.CreateDirectory(candidate);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Temp folder unavailable ({candidate}): {ex.Message}");
			}

			return Directory.Exists(candidate) ? candidate : Path.GetTempPath();
		}

		public static string GetTempFileName()
		{
			return Path.Combine(GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
		}

		public static bool IsUnderTempRoot(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return false;
			try
			{
				var full = Path.GetFullPath(path);
				var root = Path.GetFullPath(GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
				if (string.IsNullOrEmpty(root))
					return false;
				if (string.Equals(full.TrimEnd(Path.DirectorySeparatorChar), root, StringComparison.OrdinalIgnoreCase))
					return true;
				return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Deletes a managed temporary file early when it is obsolete and lives on the same
		/// volume as <paramref name="outputPath"/> (frees space before the final write).
		/// Never deletes files on removable/SD media. No-op when retain-temps is enabled.
		/// </summary>
		public static bool TryDeleteObsoleteTemporaryOnOutputVolume(
			string temporaryPath,
			string outputPath,
			Action<string> log = null)
		{
			if (_retainTemporaryFiles || string.IsNullOrWhiteSpace(temporaryPath))
				return false;
			if (!File.Exists(temporaryPath))
				return false;
			if (!IsUnderTempRoot(temporaryPath))
				return false;
			if (VolumePathHelper.IsLikelyRemovableOrSdMedia(temporaryPath))
			{
				log?.Invoke($"Keeping file on removable/SD media (not deleting): {temporaryPath}");
				return false;
			}
			if (string.IsNullOrWhiteSpace(outputPath)
				|| !VolumePathHelper.AreSameVolume(temporaryPath, outputPath))
				return false;

			FileUtils.TryDeleteFile(temporaryPath);
			if (File.Exists(temporaryPath))
				return false;

			log?.Invoke($"Deleted obsolete temporary (same volume as output): {Path.GetFileName(temporaryPath)}");
			return true;
		}

		public static void DeleteTemporaryFileUnlessRetained(string path)
		{
			if (_retainTemporaryFiles || string.IsNullOrWhiteSpace(path))
				return;
			if (VolumePathHelper.IsLikelyRemovableOrSdMedia(path) && !IsUnderTempRoot(path))
				return;
			FileUtils.TryDeleteFile(path);
		}

		public static void DeleteTemporaryDirectoryUnlessRetained(string directoryPath, bool recursive)
		{
			if (_retainTemporaryFiles || string.IsNullOrWhiteSpace(directoryPath))
				return;
			if (VolumePathHelper.IsLikelyRemovableOrSdMedia(directoryPath) && !IsUnderTempRoot(directoryPath))
				return;
			try
			{
				if (Directory.Exists(directoryPath))
					Directory.Delete(directoryPath, recursive);
			}
			catch
			{
				// ignored
			}
		}
	}
}
