
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

		public override string ToString()
		{
			var ss = $"{StartRange} - {EndRange}";
			if (!string.IsNullOrWhiteSpace(Label))
			{
				ss += $" ({Label})";
			}
			return ss;
		}
		
		public bool IsValidRange()
		{
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
		public static StartStopRange FromString(string startStopRange)
		{
			var stampString = string.Empty;
			if (string.IsNullOrWhiteSpace(startStopRange))
			{
				return null;
			}
			var label = string.Empty;
			var spaceIdx = startStopRange.IndexOf(" ");
			if (spaceIdx > 0)
			{
				stampString = startStopRange.Substring(0, spaceIdx);
				label = (startStopRange.Substring(spaceIdx) ?? "").Trim();
			}
			else
			{
				//just timestamp
				stampString = startStopRange;
			}
			if (string.IsNullOrWhiteSpace(stampString))
			{
				return null;
			}
			stampString = stampString.Trim();
			var splitStamp = stampString.Split("-");
			if (splitStamp.Length != 2)
			{
				return null;
			}
			var start = TimeRange.FromString(splitStamp[0]);
			var end = TimeRange.FromString(splitStamp[1]);
			if (start == null || end == null)
			{
				return null;
			}
			return new StartStopRange(start, end, label);
		}
	}
}
