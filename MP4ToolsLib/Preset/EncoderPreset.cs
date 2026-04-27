namespace MP4ToolsLib.Preset
{
	public class EncoderPresets
	{
		private static EncoderPreset _encoderPreset;
		public static EncoderPreset EncoderPreset
		{
			get
			{
				if (_encoderPreset == null)
				{
					_encoderPreset = DJIPreset4K30Vlog;
				}
				return _encoderPreset;
			}
			set
			{
				_encoderPreset = value;
			}
		}

		public static EncoderPreset DJIPreset4K30Vlog { get; } = new AMDH264Preset4K30();
	}

	public class DJIPreset4K30FPSTripod : EncoderPreset
	{
		// VAAPI doesn't use preset values like NVENC - leave empty
		public override string PresetV { get; } = string.Empty;

		// VAAPI doesn't use tune values - leave empty  
		public override string TuneV { get; } = string.Empty;

		public override string BV => "45M";

		public override string MaxRate => "70M";
		
		// Use hevc_vaapi for AMD VAAPI encoding
		public override string CV { get; } = "hevc_vaapi";

		public override string ID => "dji";

		// For VAAPI HEVC, valid profiles: main, main10, rext
		public override string ProfileV { get; } = "main";
	}

	
	public class AMDHevcPreset4K30 : EncoderPreset
	{
		public override string PresetV { get; } = string.Empty;

		public override string TuneV { get; } = string.Empty;

		public override string BV => "45M";

		public override string MaxRate => "70M";

		public override string CV { get; } = "hevc_vaapi";

		public override string ID => "amd-hevc-vaapi";

		// For VAAPI HEVC, valid profiles: main, main10, rext
		public override string ProfileV { get; } = "main";
	}

	public class AMDH264Preset4K30 : EncoderPreset
	{
		public override string PresetV { get; } = string.Empty;

		public override string TuneV { get; } = string.Empty;

		public override string BV => "45M";

		public override string MaxRate => "70M";

		public override string CV { get; } = "h264_vaapi";

		public override string ID => "amd-h264-vaapi";

		// For VAAPI H.264, valid profiles: main, high
		public override string ProfileV { get; } = "high";
	}

	public abstract class EncoderPreset
	{
		public abstract string ID { get; }
		public abstract string CV { get; }

		public virtual string CA { get; } = "copy";

		public virtual string RCV { get; } = "vbr";

		public abstract string BV { get; }
		public abstract string MaxRate { get; }
		public abstract string PresetV { get; }

		public abstract string TuneV { get; }

		// For H.264: baseline, main, high, high444p (NVENC); main, high (VAAPI)
		// For HEVC: main, main10, rext (VAAPI)
		public abstract string ProfileV { get; }
	}
}
