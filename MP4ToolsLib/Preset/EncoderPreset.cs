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

		public static EncoderPreset DJIPreset4K30Vlog { get; } = new AMDHevcPreset4K30();
	}


	public class AMDHevcPreset4K30 : EncoderPreset
	{
		public override string PresetV { get; } = string.Empty;

		public override string TuneV { get; } = string.Empty;

		public override string BV => "50M";

		public override string MaxRate => "70M";

		public override string CV { get; } = "hevc_vaapi";

		public override string ID => "amd-hevc-vaapi";

		// For VAAPI HEVC, valid profiles: main, main10, rext
		public override string ProfileV { get; } = "main";
	}

	public abstract class EncoderPreset
	{
		public abstract string ID { get; }
		public abstract string CV { get; }
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
