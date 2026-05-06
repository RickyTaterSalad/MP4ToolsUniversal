using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MP4ToolsLib
{
	public class TimeRange : INotifyPropertyChanged
	{
		public static IReadOnlyList<int> Range { get; } = Enumerable.Range(0, 60).ToList();

		private int _hours = 0;
		private int _minutes = 0;
		private int _seconds = 0;

		public event PropertyChangedEventHandler PropertyChanged;


		public int Hours
		{
			get => _hours;
			set
			{
				_hours = value;
				OnPropertyChanged(nameof(Hours));
			}
		}
		public int Minutes
		{
			get => _minutes;
			set
			{
				_minutes = value;
				OnPropertyChanged(nameof(Minutes));
			}
		}

		public int Seconds
		{
			get => _seconds;
			set
			{
				_seconds = value;
				OnPropertyChanged(nameof(Seconds));
			}
		}

		public double TotalSeconds => (Hours * 3600) + (Minutes * 60) + Seconds;

		public string AsInputParameterString()
		{
			return $"{Hours:00}:{Minutes:00}:{Seconds:00}";
		}

		public override string ToString()
		{
			return AsInputParameterString();
		}

		public TimeRange Clone()
		{
			return new TimeRange()
			{
				Hours = Hours,
				Minutes = Minutes,
				Seconds = Seconds
			};
		}
		
		public static TimeRange FromString(string timeRange)
		{
			if (!string.IsNullOrWhiteSpace(timeRange))
			{
				try
				{
					timeRange = timeRange.Trim();
					//	00:27:36
					var returnRange = new TimeRange();
					var split = timeRange.Trim().Split(":");
					if (split.Length == 3)
					{
						returnRange.Hours = int.Parse(split[0]);
						returnRange.Minutes = int.Parse(split[1]);
						returnRange.Seconds = int.Parse(split[2].Split(".")[0]);
						return returnRange;
					}
					else if (split.Length == 2)
					{
						returnRange.Minutes = int.Parse(split[0]);
						returnRange.Seconds = int.Parse(split[1].Split(".")[0]);
						return returnRange;
					}
					else if (split.Length == 1)
					{
						returnRange.Seconds = int.Parse(split[0].Split(".")[0]);
						return returnRange;
					}
				}
				catch
				{
					//
				}
			}
			return null;
		}
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
