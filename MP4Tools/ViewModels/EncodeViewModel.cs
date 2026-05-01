using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MP4Tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MP4Tools.Services;
using MP4ToolsLib;

namespace MP4Tools.ViewModels;

public partial class EncodeViewModel : ViewModelBase
{
	private static readonly string[] AllBitDepths = ["auto", "8 bit", "10 bit"];

	private static readonly string[] AllHardware =
		["auto", "nvenc", "qsv", "vaapi", "videotoolbox", "software"];

	private bool _applyingConstraints;

	public EncodeViewModel()
	{
		var s = AppSettingsStore.LoadOrDefault().Encoding ?? new EncodingSettingsDto();
		ReencodeOutput = s.ReencodeOutput;
		SelectedVideoCodec = NormalizeVideo(s.VideoCodec);
		SelectedAudioCodec = NormalizeAudio(s.AudioCodec);
		SelectedOutputBitDepth = NormalizeBitDepth(s.OutputBitDepth);
		SelectedHardwareAcceleration = NormalizeHw(s.HardwareAcceleration);
		ApplyEncodeConstraints();
	}

	public IReadOnlyList<string> VideoCodecChoices { get; } = ["copy", "h264", "h265"];

	public IReadOnlyList<string> AudioCodecChoices { get; } = ["copy", "aac", "libopus"];

	public ObservableCollection<string> OutputBitDepthChoicesFiltered { get; } = new();

	public ObservableCollection<string> HardwareAccelerationChoicesFiltered { get; } = new();

	/// <summary>Bit depth applies only when re-encoding a non-copy video stream.</summary>
	public bool IsOutputBitDepthEnabled => ReencodeOutput && !IsVideoCopy;

	/// <summary>Hardware video encoder / decode hint applies only when re-encoding video (not stream copy).</summary>
	public bool IsHardwareAccelerationEnabled => ReencodeOutput && !IsVideoCopy;

	private bool IsVideoCopy =>
		string.Equals(SelectedVideoCodec, "copy", StringComparison.OrdinalIgnoreCase);

	[ObservableProperty]
	private bool _reencodeOutput;

	[ObservableProperty]
	private string _selectedVideoCodec = "copy";

	[ObservableProperty]
	private string _selectedAudioCodec = "copy";

	[ObservableProperty]
	private string _selectedOutputBitDepth = "auto";

	[ObservableProperty]
	private string _selectedHardwareAcceleration = "auto";

	public string SettingsFilePathDisplay => AppSettingsStore.SettingsFilePath;

	partial void OnReencodeOutputChanged(bool value) => ApplyEncodeConstraints();

	partial void OnSelectedVideoCodecChanged(string value) => ApplyEncodeConstraints();

	partial void OnSelectedOutputBitDepthChanged(string value) => ApplyEncodeConstraints();

	partial void OnSelectedHardwareAccelerationChanged(string value) => ApplyEncodeConstraints();

	private void ApplyEncodeConstraints()
	{
		if (_applyingConstraints)
			return;

		_applyingConstraints = true;
		try
		{
			// Two passes: bit depth limits depend on HW; HW limits depend on bit depth (H.264 10-bit → software only).
			for (var pass = 0; pass < 2; pass++)
			{
				var depths = ComputeFilteredBitDepths();
				if (!depths.Contains(SelectedOutputBitDepth, StringComparer.OrdinalIgnoreCase))
					SelectedOutputBitDepth = depths[0];

				var hws = ComputeFilteredHardware();
				if (!hws.Contains(SelectedHardwareAcceleration, StringComparer.OrdinalIgnoreCase))
					SelectedHardwareAcceleration = hws[0];
			}
		}
		finally
		{
			_applyingConstraints = false;
		}

		SyncObservablePrefixThenTail(OutputBitDepthChoicesFiltered, ComputeFilteredBitDepths());
		SyncObservablePrefixThenTail(HardwareAccelerationChoicesFiltered, ComputeFilteredHardware());
		OnPropertyChanged(nameof(IsOutputBitDepthEnabled));
		OnPropertyChanged(nameof(IsHardwareAccelerationEnabled));
	}

	private static void SyncObservablePrefixThenTail(ObservableCollection<string> target, IReadOnlyList<string> source)
	{
		var prefix = 0;
		var max = Math.Min(target.Count, source.Count);
		while (prefix < max && string.Equals(target[prefix], source[prefix], StringComparison.Ordinal))
			prefix++;

		for (var i = target.Count - 1; i >= prefix; i--)
			target.RemoveAt(i);

		for (var i = prefix; i < source.Count; i++)
			target.Add(source[i]);
	}

	private IReadOnlyList<string> ComputeFilteredBitDepths()
	{
		if (!ReencodeOutput || IsVideoCopy)
			return AllBitDepths;

		if (!string.Equals(SelectedVideoCodec, "h264", StringComparison.OrdinalIgnoreCase))
			return AllBitDepths;

		// Encoder pipeline uses 8-bit for H.264 NVENC and typical HW paths; exclude 10-bit unless software encode.
		if (IsHardwareLimitedForH264TenBit(SelectedHardwareAcceleration))
			return ["auto", "8 bit"];

		return AllBitDepths;
	}

	private IReadOnlyList<string> ComputeFilteredHardware()
	{
		if (!ReencodeOutput || IsVideoCopy)
			return AllHardware;

		if (!string.Equals(SelectedVideoCodec, "h264", StringComparison.OrdinalIgnoreCase))
			return AllHardware;

		if (string.Equals(SelectedOutputBitDepth, "10 bit", StringComparison.OrdinalIgnoreCase))
			return ["software"];

		return AllHardware;
	}

	private static bool IsHardwareLimitedForH264TenBit(string hw) =>
		!string.Equals(hw, "software", StringComparison.OrdinalIgnoreCase);

	private static string NormalizeVideo(string v)
	{
		var x = (v ?? "copy").Trim().ToLowerInvariant();
		return x is "hevc" or "h.265" ? "h265" : x is "h264" or "avc" ? "h264" : x == "copy" ? "copy" : "copy";
	}

	private static string NormalizeAudio(string v)
	{
		var x = (v ?? "copy").Trim().ToLowerInvariant();
		return x switch
		{
			"opus" => "libopus",
			"aac" => "aac",
			"copy" => "copy",
			_ => "aac",
		};
	}

	private static string NormalizeBitDepth(string v)
	{
		var x = (v ?? "auto").Trim().ToLowerInvariant();
		return x is "8 bit" or "8bit" or "8" ? "8 bit" : x is "10 bit" or "10bit" or "10" ? "10 bit" : "auto";
	}

	private static string NormalizeHw(string v)
	{
		var x = (v ?? "auto").Trim().ToLowerInvariant();
		return x is "nvidia" or "cuda" ? "nvenc" : x;
	}

	[RelayCommand]
	private void SaveEncodeSettings()
	{
		try
		{
			ApplyEncodeConstraints();
			var full = AppSettingsStore.LoadOrDefault();
			full.Encoding = new EncodingSettingsDto
			{
				ReencodeOutput = ReencodeOutput,
				VideoCodec = SelectedVideoCodec ?? "copy",
				AudioCodec = SelectedAudioCodec ?? "copy",
				OutputBitDepth = SelectedOutputBitDepth ?? "auto",
				HardwareAcceleration = SelectedHardwareAcceleration ?? "auto",
			};
			AppSettingsStore.Save(full);
			EncodingSettingsRuntime.Apply(full);
			Logger.Log("Encode settings saved.");
		}
		catch (Exception ex)
		{
			Logger.Log($"Could not save encode settings: {ex.Message}");
		}
	}
}
