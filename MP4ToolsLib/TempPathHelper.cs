
using System;
using System.IO;

namespace MP4ToolsLib
{
	public static class TempPathHelper
	{
		private static string _configuredDirectory;
		private static bool _retainTemporaryFiles;

		public static bool RetainTemporaryFiles => _retainTemporaryFiles;

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

		/// <summary>Returns a unique path under the temp folder; the file is not created until a caller writes it.</summary>
		public static string GetTempFileName()
		{
			return Path.Combine(GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
		}

		public static void DeleteTemporaryFileUnlessRetained(string path)
		{
			if (_retainTemporaryFiles || string.IsNullOrWhiteSpace(path))
				return;
			FileUtils.TryDeleteFile(path);
		}

		public static void DeleteTemporaryDirectoryUnlessRetained(string directoryPath, bool recursive)
		{
			if (_retainTemporaryFiles || string.IsNullOrWhiteSpace(directoryPath))
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
