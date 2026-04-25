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
				parts.Add($"{visitorName ?? "Visitor"} vs. {homeName ?? "Home"}");
				if (visitorScore.HasValue && homeScore.HasValue)
				{
					var vScore = visitorScore.Value;
					var hScore = homeScore.Value;
					var vName = visitorName ?? "Visitor";
					var hName = homeName ?? "Home";
					
					if (vScore == hScore)
						parts.Add($"{vScore}-{hScore} Tie");
					else
					{
						var winnerName = vScore > hScore ? vName : hName;
						var winnerScore = Math.Max(vScore, hScore);
						var loserScore = Math.Min(vScore, hScore);
						parts.Add($"{winnerScore}-{loserScore} {winnerName}");
					}
				}
			}

			return string.Join(" ", parts);
		}
	}
}
