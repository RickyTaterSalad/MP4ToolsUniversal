using System.IO;
using System.Text.Json.Serialization;

namespace MP4ToolsLib
{
	public enum DrawTextPosition
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	public class StartStopRange
	{
		public StartStopRange()
		{
		}

		public StartStopRange(TimeRange startRange, string label = "")
		{
			Label = label;
			StartRange = startRange;
		}
		public StartStopRange(TimeRange startRange, TimeRange endRange, string label = "")
		{
			Label = label;
			StartRange = startRange;
			EndRange = endRange;
		}
		public string Label { get; set; }
		public DrawTextPosition SelectedDrawTextPosition { get; set; } = DrawTextPosition.TopLeft;
		public TimeRange StartRange { get; set; }
		public TimeRange EndRange { get; set; }

		/// <summary>
		/// When false, keep through EOF (no ffmpeg <c>-t</c>). Defaults to true for legacy imports.
		/// </summary>
		public bool HasEndBound { get; set; } = true;

		/// <summary>Source video this trim range applies to. Enables multi-video trim lists.</summary>
		public string InputPath { get; set; }

		[JsonIgnore]
		public string SourceFileName =>
			string.IsNullOrWhiteSpace(InputPath) ? string.Empty : Path.GetFileName(InputPath);

		[JsonIgnore]
		public string EndRangeDisplay => HasEndBound ? (EndRange?.ToString() ?? string.Empty) : "end";

		public override string ToString()
		{
			var ss = $"{StartRange} - {EndRangeDisplay}";
			if (!string.IsNullOrWhiteSpace(SourceFileName))
			{
				ss = $"{SourceFileName}: {ss}";
			}
			if (!string.IsNullOrWhiteSpace(Label))
			{
				ss += $" ({Label})";
			}
			return ss;
		}
		
		public bool IsValidRange()
		{
			if (StartRange == null)
			{
				return false;
			}
			if (!HasEndBound)
			{
				return true;
			}
			if (EndRange == null)
			{
				return false;
			}
			if (StartRange.Hours > EndRange.Hours)
			{
				return false;
			}
			if (StartRange.Hours == EndRange.Hours)
			{
				if (StartRange.Minutes > EndRange.Minutes)
				{
					return false;
				}
				if (StartRange.Minutes == EndRange.Minutes)
				{
					if (StartRange.Seconds >= EndRange.Seconds)
					{
						return false;
					}
				}
			}
			return true;
		}
	
	}
}
