using CommunityToolkit.Mvvm.ComponentModel;
using MP4ToolsLib;
using System;

namespace MP4Tools.ViewModels;

public partial class ReplaceSegmentRange : ObservableObject
{
	public Guid Id { get; } = Guid.NewGuid();

	[ObservableProperty]
	private TimeRange _startRange = new();

	[ObservableProperty]
	private TimeRange _endRange = new();

	[ObservableProperty]
	private bool _replaceWithText;

	[ObservableProperty]
	private PendingIntroSpec _pendingIntro;

	[ObservableProperty]
	private string _label = string.Empty;

	public bool HasReplacement =>
		ReplaceWithText
		&& PendingIntro != null
		&& PendingIntro.HasContent;

	public string ReplacementStatus
	{
		get
		{
			if (!ReplaceWithText)
				return "Remove only";
			if (HasReplacement)
				return PendingIntro.DisplaySummary;
			return "Needs intro";
		}
	}

	public double StartSeconds => StartRange?.TotalSeconds ?? 0;
	public double EndSeconds => EndRange?.TotalSeconds ?? 0;

	public bool IsValid =>
		StartRange != null
		&& EndRange != null
		&& EndSeconds > StartSeconds;

	partial void OnReplaceWithTextChanged(bool value)
	{
		OnPropertyChanged(nameof(ReplacementStatus));
		OnPropertyChanged(nameof(HasReplacement));
		if (!value)
			PendingIntro = null;
	}

	partial void OnPendingIntroChanged(PendingIntroSpec value)
	{
		OnPropertyChanged(nameof(ReplacementStatus));
		OnPropertyChanged(nameof(HasReplacement));
	}

	partial void OnStartRangeChanged(TimeRange value) => WireTimeRange(value);
	partial void OnEndRangeChanged(TimeRange value) => WireTimeRange(value);

	private void WireTimeRange(TimeRange range)
	{
		if (range == null)
			return;
		range.PropertyChanged -= OnTimePartChanged;
		range.PropertyChanged += OnTimePartChanged;
	}

	private void OnTimePartChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		OnPropertyChanged(nameof(StartSeconds));
		OnPropertyChanged(nameof(EndSeconds));
		OnPropertyChanged(nameof(IsValid));
	}

	public ReplaceSegmentRange()
	{
		WireTimeRange(StartRange);
		WireTimeRange(EndRange);
	}

	public override string ToString()
	{
		var core = $"{StartRange} – {EndRange}";
		return string.IsNullOrWhiteSpace(Label) ? $"{core} ({ReplacementStatus})" : $"{core} [{Label}] ({ReplacementStatus})";
	}
}
