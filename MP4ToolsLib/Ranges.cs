
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
	}
}
