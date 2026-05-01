using MP4ToolsLib;
using Xunit;

namespace MP4ToolsLib.Tests;

public sealed class StartStopRangeTests
{
	[Fact]
	public void Valid_range_inside_ten_minute_clip_example()
	{
		var start = new TimeRange { Hours = 0, Minutes = 5, Seconds = 0 };
		var end = new TimeRange { Hours = 0, Minutes = 9, Seconds = 30 };
		var range = new StartStopRange(start, end);
		Assert.True(range.IsValidRange());
	}

	[Fact]
	public void Invalid_when_start_not_before_end()
	{
		var a = new TimeRange { Hours = 0, Minutes = 8, Seconds = 0 };
		var b = new TimeRange { Hours = 0, Minutes = 8, Seconds = 0 };
		Assert.False(new StartStopRange(a, b).IsValidRange());
	}
}
