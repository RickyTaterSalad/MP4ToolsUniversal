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
					/*
					if (!string.IsNullOrWhiteSpace(Settings.Default.DefaultEncoder) && Settings.Default.DefaultEncoder.Equals("gopro", System.StringComparison.InvariantCultureIgnoreCase))
					{
						_encoderPreset = GoProPreset4K30FPSTripod;
					}
					else
					{
					*/
					_encoderPreset = DJIPreset4K30Vlog;
					//	}
				}
				return _encoderPreset;
			}
			set
			{
				_encoderPreset = value;
				if (_encoderPreset != null)
				{
					/*
					if (string.IsNullOrWhiteSpace(Settings.Default.DefaultEncoder) ||
						!Settings.Default.DefaultEncoder.Equals(_encoderPreset.ID, System.StringComparison.InvariantCultureIgnoreCase))
					{
						Settings.Default.DefaultEncoder = _encoderPreset.ID;
						Settings.Default.Save();
					}
					*/
				}
			}
		}

		public static EncoderPreset DJIPreset4K30Vlog { get; } = new DJIPreset4K30FPSTripod();

		public static EncoderPreset GoProPreset4K30FPSTripod { get; } = new GoProPreset4K30FPSTripod();
	}
	public class DJIPreset4K30FPSTripod : EncoderPreset
	{
		public override string PresetV { get; } = "p7";

		public override string TuneV { get; } = "hq";

		public override string BV => "45M";

		public override string MaxRate => "70M";
		public override string CV { get; } = "hevc_nvenc";

		public override string ID => "dji";
	}
	public class GoProPreset4K30FPSTripod : EncoderPreset
	{
		public override string PresetV { get; } = "p7";

		public override string TuneV { get; } = "hq";

		public override string BV => "45M";

		public override string MaxRate => "70M";

		public override string CV { get; } = "h264_nvenc";

		public override string ID => "gopro";
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

		public string ProfileV { get; } = "0";
	}
}
