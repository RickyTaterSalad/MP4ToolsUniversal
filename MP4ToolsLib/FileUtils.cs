using System;
using System.Collections.Generic;
using System.IO;

namespace MP4ToolsLib
{
	public class FileUtils
	{
		public static void TryDeleteFile(string path)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
				{
					File.Delete(path);
				}
			}
			catch
			{
			}
		}

		public static string BuildGameInfoOutputFileName(DateTime? eventDate, string visitorName, string homeName)
		{
			if (!eventDate.HasValue)
			{
				return string.Empty;
			}
			var datePart = eventDate.Value.ToString("yyyy-MM-dd");
			var parts = new List<string> { datePart };

			if (!string.IsNullOrWhiteSpace(visitorName) || !string.IsNullOrWhiteSpace(homeName))
			{
				parts.Add($"{visitorName ?? "Visitor"} vs. {homeName ?? "Home"}");
			}

			// Strip periods from the stem (e.g. "vs.") and normalize hyphens; callers add the extension separately.
			return string.Join(" ", parts).Replace(".", string.Empty).Replace("-", "_");
		}
	}
}
