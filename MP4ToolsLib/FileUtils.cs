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

		public static string BuildGameInfoOutputFileName(DateTime? eventDate, string visitorName, string homeName, int? visitorScore, int? homeScore)
		{
			if (!eventDate.HasValue)
			{
				return string.Empty;
			}
			var datePart = eventDate.Value.ToString("yyyy-MM-dd");
			var parts = new List<string> { datePart };

			if (!string.IsNullOrWhiteSpace(visitorName) || !string.IsNullOrWhiteSpace(homeName))
			{
				parts.Add($"{visitorName} vs. {homeName}");
				if (visitorScore.HasValue && homeScore.HasValue)
				{
					{
						var winnerName = visitorScore > homeScore ? visitorName : homeName;
						var winnerScore = Math.Max(visitorScore.Value, homeScore.Value);
						var loserScore = Math.Min(visitorScore.Value, homeScore.Value);
						parts.Add($"{winnerScore}-{loserScore} {winnerName}");
					}
				}
			}

			return string.Join(" ", parts);
		}
	}
}
