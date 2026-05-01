using System;
using System.Collections.Generic;
using System.Text;

namespace MP4ToolsLib.FFmpegArguments
{
    public class FFmpegArgumentBuilder
    {
        private readonly List<ArgumentGroup> _groups = new();
        private readonly List<string> _globalOptions = new();
        private string _hwaccel;
        private string _vaapiDevice;

        public FFmpegArgumentBuilder AddGlobalOption(string option)
        {
            _globalOptions.Add(option);
            return this;
        }

        public FFmpegArgumentBuilder WithHardwareAccel(string hwaccel)
        {
            _hwaccel = hwaccel;
            return this;
        }

        public FFmpegArgumentBuilder WithVaapiDevice(string device)
        {
            _vaapiDevice = device;
            return this;
        }

        public FFmpegArgumentBuilder AddInput(string filePath, Action<InputGroup> configure = null)
        {
            var group = new InputGroup(filePath);
            configure?.Invoke(group);
            _groups.Add(group);
            return this;
        }

        public FFmpegArgumentBuilder AddOutput(string filePath, Action<OutputGroup> configure = null)
        {
            var group = new OutputGroup(filePath);
            configure?.Invoke(group);
            _groups.Add(group);
            return this;
        }

        public FFmpegArgumentBuilder AddConcatInput(string fileListPath, Action<ConcatInputGroup> configure = null)
        {
            var group = new ConcatInputGroup(fileListPath);
            configure?.Invoke(group);
            _groups.Add(group);
            return this;
        }

        public FFmpegArgumentBuilder AddConcatProtocol(string concatenatedPaths, Action<ConcatProtocolGroup> configure = null)
        {
            var group = new ConcatProtocolGroup(concatenatedPaths);
            configure?.Invoke(group);
            _groups.Add(group);
            return this;
        }

        public FFmpegArgumentBuilder AddRawArguments(string arguments)
        {
            _groups.Add(new RawArgumentGroup(arguments));
            return this;
        }

        public string Build()
        {
            var sb = new StringBuilder();

            foreach (var opt in _globalOptions)
                AppendIfNeeded(sb, opt);

            if (!string.IsNullOrWhiteSpace(_vaapiDevice))
                AppendIfNeeded(sb, $"-vaapi_device {_vaapiDevice}");
            if (!string.IsNullOrWhiteSpace(_hwaccel))
                AppendIfNeeded(sb, $"-hwaccel {_hwaccel}");

            foreach (var group in _groups)
            {
                var groupArgs = group.Build();
                if (!string.IsNullOrWhiteSpace(groupArgs))
                    AppendIfNeeded(sb, groupArgs);
            }

            return sb.ToString().Trim();
        }

        private static void AppendIfNeeded(StringBuilder sb, string value)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(value);
        }

        internal string ApplyForcedArgs(string args)
        {
            if (!args.Contains("-nostdin", StringComparison.OrdinalIgnoreCase))
                args = $"-nostdin {args}".Trim();
            return args;
        }
    }

    public abstract class ArgumentGroup
    {
        public readonly List<FlagArgument> _flags = new();
        public readonly List<KeyValueArgument> _kvArgs = new();
        public readonly List<FilterArgument> _filters = new();
        public readonly List<MapArgument> _maps = new();
        public readonly List<BitstreamFilter> _bsfs = new();
        public readonly List<string> _rawParts = new();

        public ArgumentGroup AddFlag(string flag) { _flags.Add(new FlagArgument(flag)); return this; }
        public ArgumentGroup AddKeyValue(string key, string value) { _kvArgs.Add(new KeyValueArgument(key, value)); return this; }
        public ArgumentGroup AddVideoFilter(string filter) { _filters.Add(new FilterArgument("vf", filter)); return this; }
        public ArgumentGroup AddAudioFilter(string filter) { _filters.Add(new FilterArgument("af", filter)); return this; }
        public ArgumentGroup AddComplexFilter(string filter) { _filters.Add(new FilterArgument("filter_complex", filter)); return this; }
        public ArgumentGroup AddMap(string mapping) { _maps.Add(new MapArgument(mapping)); return this; }
        public ArgumentGroup AddBitstreamFilter(string streamSpecifier, string filter) { _bsfs.Add(new BitstreamFilter(streamSpecifier, filter)); return this; }

        public virtual string Build()
        {
            var sb = new StringBuilder();
            foreach (var f in _flags) AppendIfNeeded(sb, f.Build());
            foreach (var kv in _kvArgs) AppendIfNeeded(sb, kv.Build());
            foreach (var bsf in _bsfs) AppendIfNeeded(sb, bsf.Build());

            foreach (var map in _maps) AppendIfNeeded(sb, map.Build());
            foreach (var filter in _filters) AppendIfNeeded(sb, filter.Build());
            return sb.ToString();
        }

        public static void AppendIfNeeded(StringBuilder sb, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(value);
            }
        }
    }

    public class InputGroup : ArgumentGroup
    {
        private readonly string _filePath;
        private string _seekStart;
        private string _duration;
        private string _format;

        internal InputGroup(string filePath) { _filePath = filePath; }

        public InputGroup SeekTo(string time) { _seekStart = time; return this; }
        public InputGroup Duration(string duration) { _duration = duration; return this; }
        public InputGroup WithFormat(string format) { _format = format; return this; }

        public override string Build()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(_seekStart))
                sb.Append($"-ss {_seekStart}");

            if (!string.IsNullOrWhiteSpace(_format))
                AppendIfNeeded(sb, $"-f {_format}");

            AppendIfNeeded(sb, $"-i \"{_filePath}\"");

            if (!string.IsNullOrWhiteSpace(_duration))
                AppendIfNeeded(sb, $"-t {_duration}");

            var baseArgs = base.Build();
            if (!string.IsNullOrWhiteSpace(baseArgs))
                AppendIfNeeded(sb, baseArgs);

            return sb.ToString();
        }
    }

    public class OutputGroup : ArgumentGroup
    {
        private readonly string _filePath;
        private string _format;
        private string _movflags;
        private string _shortest;
        private string _pixelFormat;
        private string _presetV;
        private string _tuneV;
        private string _rcV;
        private string _profileV;

        internal OutputGroup(string filePath) { _filePath = filePath; }

        public OutputGroup WithFormat(string format) { _format = format; return this; }
        public OutputGroup AsMpegTs() => WithFormat("mpegts");
        public OutputGroup AsMp4() => WithFormat("mp4");
        public OutputGroup WithMovFlags(string movflags) { _movflags = movflags; return this; }
        public OutputGroup Shortest() { _shortest = "-shortest"; return this; }
        public OutputGroup WithPixelFormat(string pixelFormat) { _pixelFormat = pixelFormat; return this; }
        public OutputGroup WithVideoCodec(string codec) { AddKeyValue("c:v", codec); return this; }
        public OutputGroup WithAudioCodec(string codec) { AddKeyValue("c:a", codec); return this; }
        public OutputGroup WithVideoPreset(string preset) { _presetV = preset; return this; }
        public OutputGroup WithVideoTune(string tune) { _tuneV = tune; return this; }
        public OutputGroup WithRateControl(string rc) { _rcV = rc; return this; }
        public OutputGroup WithVideoProfile(string profile) { _profileV = profile; return this; }
        public OutputGroup WithVideoBitrate(string bitrate) { AddKeyValue("b:v", bitrate); return this; }
        public OutputGroup WithMaxVideoBitrate(string maxrate) { AddKeyValue("maxrate", maxrate); return this; }

        public override string Build()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(_format))
                sb.Append($"-f {_format}");

            if (!string.IsNullOrWhiteSpace(_movflags))
                AppendIfNeeded(sb, $"-movflags {_movflags}");

            var baseArgs = base.Build();
            if (!string.IsNullOrWhiteSpace(baseArgs))
                AppendIfNeeded(sb, baseArgs);

            if (!string.IsNullOrWhiteSpace(_pixelFormat))
                AppendIfNeeded(sb, $"-pix_fmt {_pixelFormat}");
            if (!string.IsNullOrWhiteSpace(_presetV))
                AppendIfNeeded(sb, $"-preset:v {_presetV}");
            if (!string.IsNullOrWhiteSpace(_tuneV))
                AppendIfNeeded(sb, $"-tune:v {_tuneV}");
            if (!string.IsNullOrWhiteSpace(_rcV))
                AppendIfNeeded(sb, $"-rc:v {_rcV}");
            if (!string.IsNullOrWhiteSpace(_profileV))
                AppendIfNeeded(sb, $"-profile:v {_profileV}");

            if (!string.IsNullOrWhiteSpace(_shortest))
                AppendIfNeeded(sb, _shortest);

            AppendIfNeeded(sb, $"\"{_filePath}\"");

            return sb.ToString();
        }
    }

    public class ConcatInputGroup : ArgumentGroup
    {
        private readonly string _fileListPath;

        internal ConcatInputGroup(string fileListPath) { _fileListPath = fileListPath; }

        public override string Build()
        {
            var sb = new StringBuilder();
            sb.Append("-f concat -safe 0");
            var baseArgs = base.Build();
            if (!string.IsNullOrWhiteSpace(baseArgs))
                AppendIfNeeded(sb, baseArgs);
            AppendIfNeeded(sb, $"-i \"{_fileListPath}\"");
            return sb.ToString();
        }
    }

    public class ConcatProtocolGroup : ArgumentGroup
    {
        private readonly string _concatenatedPaths;

        internal ConcatProtocolGroup(string concatenatedPaths) { _concatenatedPaths = concatenatedPaths; }

        public override string Build()
        {
            var sb = new StringBuilder();
            var baseArgs = base.Build();
            if (!string.IsNullOrWhiteSpace(baseArgs))
                AppendIfNeeded(sb, baseArgs);
            sb.Append($"-i \"concat:{_concatenatedPaths}\"");
            return sb.ToString();
        }
    }

    public class RawArgumentGroup : ArgumentGroup
    {
        private readonly string _raw;

        internal RawArgumentGroup(string raw) { _raw = raw; }

        public override string Build() => _raw;
    }

    public struct FlagArgument
    {
        public string Flag { get; }
        public FlagArgument(string flag) => Flag = flag;
        public string Build() => Flag;
    }

    public struct KeyValueArgument
    {
        public string Key { get; }
        public string Value { get; }
        public KeyValueArgument(string key, string value)
        {
            Key = key;
            Value = value;
        }
        public string Build() => $"-{Key} {Value}";
    }

    public struct FilterArgument
    {
        public string FilterType { get; }
        public string Filter { get; }
        public FilterArgument(string filterType, string filter)
        {
            FilterType = filterType;
            Filter = filter;
        }
        public string Build() => $"-{FilterType} \"{Filter}\"";
    }

    public struct MapArgument
    {
        public string Mapping { get; }
        public MapArgument(string mapping) => Mapping = mapping;
        public string Build() => $"-map {Mapping}";
    }

    public struct BitstreamFilter
    {
        public string StreamSpecifier { get; }
        public string Filter { get; }
        public BitstreamFilter(string streamSpecifier, string filter)
        {
            StreamSpecifier = streamSpecifier;
            Filter = filter;
        }
        public string Build() => $"-bsf:{StreamSpecifier} {Filter}";
    }
}