using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib
{
    public static class IntroVideoComposerAsync
    {
        private static string _font;
        private static string Font
        {
            get
            {
                if (_font == null)
                {
                    _font = "C\\:/Windows/Fonts/seguiemj.ttf";
                    if (OperatingSystem.IsLinux())
                    {
                        // Try multiple common font paths
                        var possibleFonts = new[]
                        {
                            "/usr/share/fonts/truetype/noto/NotoSansMono-Regular.ttf",
                            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf"
                        };

                        foreach (var fontPath in possibleFonts)
                        {
                            if (File.Exists(fontPath))
                            {
                                _font = fontPath;
                                break;
                            }
                        }
                    }
                }
                return _font;
            }
        }

        /// <summary>
        /// Strip an existing intro of <paramref name="existingIntroDurationSeconds"/> from
        /// <paramref name="inputPath"/> (stream-copy seek) and prepend a new intro of the same
        /// length with the given text. Does not re-encode the main footage.
        /// </summary>
        public static Task ReplaceIntroAsync(
            string inputPath,
            string titleText,
            string subtitleText,
            string outputPath,
            int existingIntroDurationSeconds,
            int titleFontSize = 128,
            int subtitleFontSize = 64,
            int detailsFontSize = 48,
            string detailsText = "",
            Action<string> log = null,
            IProgress<double> progress = null,
            CancellationToken ct = default,
            Action<string> operationStep = null,
            string hwAccelForIntro = null,
            bool resolveSafeEncoding = false)
        {
            if (existingIntroDurationSeconds < 1)
                throw new ArgumentOutOfRangeException(nameof(existingIntroDurationSeconds), "Existing intro duration must be at least 1 second.");

            var seekTs = TimeSpan.FromSeconds(existingIntroDurationSeconds);
            var seekOpt = FfmpegOption.Pair(
                FfmpegArguments.SeekInputTimestamp,
                $"{(int)seekTs.TotalHours:00}:{seekTs.Minutes:00}:{seekTs.Seconds:00}");

            log ??= _ => { };
            log($"Rewriting intro: seeking past {existingIntroDurationSeconds}s, then prepending new intro…");
            operationStep?.Invoke("Stripping existing intro…");

            return PrependIntroAsync(
                inputPath,
                titleText,
                subtitleText,
                outputPath,
                durationSeconds: existingIntroDurationSeconds,
                titleFontSize: titleFontSize,
                subtitleFontSize: subtitleFontSize,
                detailsFontSize: detailsFontSize,
                detailsText: detailsText,
                log: log,
                progress: progress,
                ct: ct,
                operationStep: operationStep,
                useTrimSegmentAudioCodec: false,
                seekBeforeMainInput: seekOpt,
                hwAccelForIntro: hwAccelForIntro,
                resolveSafeEncoding: resolveSafeEncoding,
                deleteSourceAfterRemux: true);
        }

        /// <summary>
        /// Encodes a standalone intro MP4 matched to <paramref name="referenceVideoPath"/>
        /// (resolution, fps, video/audio codecs) so Combine can stream-copy-concat it with no re-encode.
        /// </summary>
        public static async Task GenerateIntroAsync(
            string referenceVideoPath,
            string outputPath,
            string titleText,
            string subtitleText = "",
            string detailsText = "",
            int durationSeconds = 10,
            int titleFontSize = 128,
            int subtitleFontSize = 64,
            int detailsFontSize = 48,
            int lineGap = 36,
            string backgroundColor = "0x1E1E1E",
            string textColor = "white",
            bool scaleFontsToResolution = true,
            Action<string> log = null,
            CancellationToken ct = default,
            Action<string> operationStep = null)
        {
            log ??= _ => { };
            void Step(string message) => operationStep?.Invoke(message);

            if (string.IsNullOrWhiteSpace(referenceVideoPath) || !File.Exists(referenceVideoPath))
                throw new FileNotFoundException("Reference video not found.", referenceVideoPath);
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path is required.", nameof(outputPath));
            if (durationSeconds < 1)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));

            var outDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outDir) && !Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            Step("Probing reference video…");
            var (video, audio) = await FFMpegUtils.Instance.ProbeMediaInfoAsync(referenceVideoPath, ct, log);

            string w = !string.IsNullOrWhiteSpace(video?.Width) ? video.Width : "1920";
            string h = !string.IsNullOrWhiteSpace(video?.Height) ? video.Height : "1080";
            string fps = !string.IsNullOrWhiteSpace(video?.FrameRate) ? video.FrameRate : "30000/1001";
            string vCodecProbed = (!string.IsNullOrWhiteSpace(video?.CodecName) ? video.CodecName : "hevc").ToLowerInvariant();
            string ar = !string.IsNullOrWhiteSpace(audio?.SampleRate) ? audio.SampleRate : "48000";
            string ach = !string.IsNullOrWhiteSpace(audio?.Channels) ? audio.Channels : "2";
            string acl = !string.IsNullOrWhiteSpace(audio?.ChannelLayout) ? audio.ChannelLayout : (ach == "1" ? "mono" : "stereo");
            string aCodecProbed = (!string.IsNullOrWhiteSpace(audio?.CodecName) ? audio.CodecName : "aac").ToLowerInvariant();

            var videoEncoder = IntroCodecMatching.ResolveVideoEncoder(vCodecProbed);
            var audioEncoder = IntroCodecMatching.ResolveAudioEncoder(aCodecProbed);
            log($"Reference {w}x{h} @{fps}, video {vCodecProbed}→{videoEncoder}, audio {aCodecProbed}→{audioEncoder}");

            var bg = NormalizeLavfiColor(backgroundColor);
            var fg = NormalizeDrawtextColor(textColor);

            var titleSize = titleFontSize;
            var subtitleSize = subtitleFontSize;
            var detailsSize = detailsFontSize;
            var gap = Math.Max(1, lineGap);
            if (scaleFontsToResolution)
            {
                var resolution = await FFMpegUtils.Instance.GetVideoResolutionViaFfmpegAsync(referenceVideoPath, ct, log);
                var fontScale = IntroVideoFontScaling.GetFontScale(
                    resolution?.width ?? 0,
                    resolution?.height ?? 0);
                if (resolution.HasValue)
                    log($"Reference resolution {resolution.Value.width}x{resolution.Value.height}; font scale {fontScale:P0}");
                titleSize = IntroVideoFontScaling.ScaleFontSize(titleFontSize, fontScale);
                subtitleSize = IntroVideoFontScaling.ScaleFontSize(subtitleFontSize, fontScale);
                detailsSize = IntroVideoFontScaling.ScaleFontSize(detailsFontSize, fontScale);
                gap = Math.Max(1, (int)Math.Round(lineGap * fontScale, MidpointRounding.AwayFromZero));
            }

            var vf = BuildIntroDrawtextFilter(titleText, subtitleText, detailsText, fg, titleSize, subtitleSize, detailsSize, gap);

            Step("Encoding intro (matched to reference)…");
            log($"Creating intro → {outputPath}");

            var parts = new List<FfmpegOption?>
            {
                FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
                FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
                FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatLavfi),
                FfmpegCommandLine.DefaultInputThreadQueue(),
                FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted($"color=c={bg}:s={w}x{h}:r={fps}:d={durationSeconds}")),
                FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatLavfi),
                FfmpegCommandLine.DefaultInputThreadQueue(),
                FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted($"anullsrc=r={ar}:cl={acl}:d={durationSeconds}")),
                FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{vf}\""),
                FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, videoEncoder),
                FfmpegOption.Pair(FfmpegArguments.OutputVideoFrameRate, fps),
                FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, audioEncoder),
            };
            if (!string.Equals(audioEncoder, FfmpegArguments.StreamCopy, StringComparison.OrdinalIgnoreCase)
                && !audioEncoder.StartsWith("pcm_", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(FfmpegOption.Pair(FfmpegArguments.AudioBitrate, "192k"));
            }
            parts.Add(FfmpegOption.Pair(FfmpegArguments.AudioSampleRate, ar));
            parts.Add(FfmpegOption.Pair(FfmpegArguments.AudioChannels, ach));
            if (string.Equals(videoEncoder, "libx265", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(FfmpegOption.Pair(FfmpegArguments.ConstantRateFactor, "18"));
                parts.Add(FfmpegOption.Pair(FfmpegArguments.EncoderPreset, "fast"));
                parts.Add(FfmpegOption.Pair("-tag:v", "hvc1"));
            }
            else if (string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(FfmpegOption.Pair(FfmpegArguments.ConstantRateFactor, "18"));
                parts.Add(FfmpegOption.Pair(FfmpegArguments.EncoderPreset, "fast"));
            }
            parts.Add(FfmpegOption.Unary(FfmpegArguments.StopEncodingWhenShortestStreamEnds));
            parts.Add(FfmpegOption.Pair(FfmpegArguments.MuxerFlags, "+faststart"));
            parts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(outputPath)));

            await FFMpegUtils.Instance.RunCaptureFFMpegAsync(FfmpegCommandLine.Build(parts), ct, log);

            if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
                throw new InvalidOperationException("Intro file was not created.");

            log("Intro generated.");
            Step("Done.");
        }

        /// <summary>
        /// Renders a single JPEG still of the intro card (same drawtext styling as
        /// <see cref="GenerateIntroAsync"/>) for UI preview. Uses reference resolution when available.
        /// </summary>
        public static async Task<string> RenderIntroPreviewAsync(
            string referenceVideoPath,
            string titleText,
            string subtitleText = "",
            string detailsText = "",
            int titleFontSize = 128,
            int subtitleFontSize = 64,
            int detailsFontSize = 48,
            int lineGap = 36,
            string backgroundColor = "0x1E1E1E",
            string textColor = "white",
            bool scaleFontsToResolution = true,
            int maxPreviewWidth = 960,
            Action<string> log = null,
            CancellationToken ct = default)
        {
            log ??= _ => { };

            string w = "1920";
            string h = "1080";
            int widthPx = 1920;
            int heightPx = 1080;

            if (!string.IsNullOrWhiteSpace(referenceVideoPath) && File.Exists(referenceVideoPath))
            {
                var (video, _) = await FFMpegUtils.Instance.ProbeMediaInfoAsync(referenceVideoPath, ct, log);
                if (!string.IsNullOrWhiteSpace(video?.Width))
                    w = video.Width;
                if (!string.IsNullOrWhiteSpace(video?.Height))
                    h = video.Height;
                int.TryParse(w, out widthPx);
                int.TryParse(h, out heightPx);
                if (widthPx <= 0) widthPx = 1920;
                if (heightPx <= 0) heightPx = 1080;
            }

            var bg = NormalizeLavfiColor(backgroundColor);
            var fg = NormalizeDrawtextColor(textColor);
            var titleSize = titleFontSize;
            var subtitleSize = subtitleFontSize;
            var detailsSize = detailsFontSize;
            var gap = Math.Max(1, lineGap);
            if (scaleFontsToResolution)
            {
                var fontScale = IntroVideoFontScaling.GetFontScale(widthPx, heightPx);
                titleSize = IntroVideoFontScaling.ScaleFontSize(titleFontSize, fontScale);
                subtitleSize = IntroVideoFontScaling.ScaleFontSize(subtitleFontSize, fontScale);
                detailsSize = IntroVideoFontScaling.ScaleFontSize(detailsFontSize, fontScale);
                gap = Math.Max(1, (int)Math.Round(lineGap * fontScale, MidpointRounding.AwayFromZero));
            }

            var displayTitle = string.IsNullOrWhiteSpace(titleText) ? "(title)" : titleText;
            var vf = BuildIntroDrawtextFilter(displayTitle, subtitleText, detailsText, fg, titleSize, subtitleSize, detailsSize, gap);
            if (maxPreviewWidth > 0 && widthPx > maxPreviewWidth)
                vf += $",scale={maxPreviewWidth}:-1";

            var outPath = Path.Combine(TempPathHelper.GetTempPath(), $"intro_preview_{Guid.NewGuid():N}.jpg");
            var parts = new List<FfmpegOption?>
            {
                FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
                FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
                FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatLavfi),
                FfmpegCommandLine.DefaultInputThreadQueue(),
                FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted($"color=c={bg}:s={w}x{h}:r=1:d=1")),
                FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{vf}\""),
                FfmpegOption.Pair(FfmpegArguments.OutputVideoFrameCount, "1"),
                FfmpegOption.Pair(FfmpegArguments.OutputImageQuality, "3"),
                FfmpegOption.Positional(FfmpegCommandLine.Quoted(outPath)),
            };

            await FFMpegUtils.Instance.RunCaptureFFMpegAsync(FfmpegCommandLine.Build(parts), ct, log);

            if (!File.Exists(outPath) || new FileInfo(outPath).Length == 0)
                throw new InvalidOperationException("Intro preview image was not created.");

            return outPath;
        }

        private static string BuildIntroDrawtextFilter(
            string titleText,
            string subtitleText,
            string detailsText,
            string textColor,
            int titleFontSize,
            int subtitleFontSize,
            int detailsFontSize,
            int lineGap)
        {
            var escapedTitle = EscapeDrawtext(titleText);
            var escapedSubtitle = EscapeDrawtext(subtitleText);
            var escapedDetails = EscapeDrawtext(detailsText);
            var gap = Math.Max(1, lineGap);
            var vf = $"drawtext=text='{escapedTitle}':fontfile='{Font}':fontcolor={textColor}:fontsize={titleFontSize}:x=(w-text_w)/2:y=(h/2)-text_h-{gap / 2}";
            if (!string.IsNullOrWhiteSpace(escapedSubtitle))
                vf += $",drawtext=text='{escapedSubtitle}':fontfile='{Font}':fontcolor={textColor}:fontsize={subtitleFontSize}:x=(w-text_w)/2:y=(h/2)+{gap / 2}";
            if (!string.IsNullOrWhiteSpace(escapedDetails))
                vf += $",drawtext=text='{escapedDetails}':fontfile='{Font}':fontcolor={textColor}:fontsize={detailsFontSize}:x=(w-text_w)/2:y=(h/2)+{gap / 2}+{subtitleFontSize}+{gap}";
            return vf;
        }

        private static string NormalizeLavfiColor(string color)
        {
            var c = (color ?? "0x1E1E1E").Trim();
            if (c.StartsWith("#", StringComparison.Ordinal))
                c = "0x" + c[1..];
            return c;
        }

        private static string NormalizeDrawtextColor(string color)
        {
            var c = (color ?? "white").Trim();
            if (c.StartsWith("#", StringComparison.Ordinal))
                return "0x" + c[1..];
            return c;
        }

        public static async Task PrependIntroAsync(
            string inputPath,
            string titleText,
            string subtitleText,
            string outputPath,
            int durationSeconds = 10,
            int titleFontSize = 128,
            int subtitleFontSize = 64,
            int detailsFontSize = 48,
            string detailsText = "",
            Action<string> log = null,
            IProgress<double> progress = null, // 0..1
            CancellationToken ct = default,
            Action<string> operationStep = null,
            bool useTrimSegmentAudioCodec = false,
            FfmpegOption seekBeforeMainInput = default,
            string hwAccelForIntro = null,
            bool resolveSafeEncoding = true,
            bool deleteSourceAfterRemux = false
            )
        {
            log ??= _ => { };
            void Step(string message) => operationStep?.Invoke(message);
            if (!File.Exists(inputPath))
                throw new FileNotFoundException(inputPath);

            string introTs = Path.Combine(TempPathHelper.GetTempPath(), $"intro_{Guid.NewGuid():N}.ts");
            string inputTs = Path.Combine(TempPathHelper.GetTempPath(), $"input_{Guid.NewGuid():N}.ts");

            try
            {
                var (video, audio) = await FFMpegUtils.Instance.ProbeMediaInfoAsync(inputPath, ct, log);

                string w = !string.IsNullOrWhiteSpace(video?.Width) ? video.Width : "1920";
                string h = !string.IsNullOrWhiteSpace(video?.Height) ? video.Height : "1080";
                string fps = !string.IsNullOrWhiteSpace(video?.FrameRate) ? video.FrameRate : "30000/1001";
                string vCodec = (!string.IsNullOrWhiteSpace(video?.CodecName) ? video.CodecName : "h265").ToLowerInvariant();

                string ar = !string.IsNullOrWhiteSpace(audio?.SampleRate) ? audio.SampleRate : "48000";
                string ach = !string.IsNullOrWhiteSpace(audio?.Channels) ? audio.Channels : "2";
                string acl = !string.IsNullOrWhiteSpace(audio?.ChannelLayout) ? audio.ChannelLayout : (ach == "1" ? "mono" : "stereo");
                string aCodec = (!string.IsNullOrWhiteSpace(audio?.CodecName) ? audio.CodecName : "aac").ToLowerInvariant();

                // Trim re-encodes audio (libopus). Combine matches source so the main clip can stay -c:a copy.
                string introAudioEnc = useTrimSegmentAudioCodec
                    ? "libopus"
                    : (aCodec.Contains("opus") ? "libopus" : "aac");

                var escapedTitle = EscapeDrawtext(titleText);
                var escapedSubtitle = EscapeDrawtext(subtitleText);
                var escapedDetails = EscapeDrawtext(detailsText);

                var resolution = await FFMpegUtils.Instance.GetVideoResolutionViaFfmpegAsync(inputPath, ct, log);
                var fontScale = IntroVideoFontScaling.GetFontScale(
                    resolution?.width ?? 0,
                    resolution?.height ?? 0);
                if (resolution.HasValue)
                    log($"Input resolution {resolution.Value.width}x{resolution.Value.height}; intro font scale {fontScale:P0}");
                else
                    log($"Could not read input resolution via ffmpeg; intro font scale {fontScale:P0}");

                titleFontSize = IntroVideoFontScaling.ScaleFontSize(titleFontSize, fontScale);
                subtitleFontSize = IntroVideoFontScaling.ScaleFontSize(subtitleFontSize, fontScale);
                detailsFontSize = IntroVideoFontScaling.ScaleFontSize(detailsFontSize, fontScale);
                var lineGap = IntroVideoFontScaling.ScaleLineGap(fontScale);
                var vf = $"drawtext=text='{escapedTitle}':fontfile='{Font}':fontcolor=white:fontsize={titleFontSize}:x=(w-text_w)/2:y=(h/2)-text_h-{lineGap / 2}";
                if (!string.IsNullOrWhiteSpace(escapedSubtitle))
                {
                    vf += $",drawtext=text='{escapedSubtitle}':fontfile='{Font}':fontcolor=white:fontsize={subtitleFontSize}:x=(w-text_w)/2:y=(h/2)+{lineGap / 2}";
                }
                if (!string.IsNullOrWhiteSpace(escapedDetails))
                {
                    vf += $",drawtext=text='{escapedDetails}':fontfile='{Font}':fontcolor=white:fontsize={detailsFontSize}:x=(w-text_w)/2:y=(h/2)+{lineGap / 2}+{subtitleFontSize}+{lineGap}";
                }
                var encodeTenBit = resolveSafeEncoding && VideoBitDepthHelper.IsTenBitVideo(video);
                var hwUploadSuffix = resolveSafeEncoding
                    ? DaVinciOutputEncoding.GetSoftwareToVaapiUploadFilterSuffix(hwAccelForIntro, encodeTenBit)
                    : (DaVinciOutputEncoding.UsesVaapi(hwAccelForIntro) && OperatingSystem.IsLinux() ? ",format=nv12,hwupload" : string.Empty);
                if (!string.IsNullOrEmpty(hwUploadSuffix))
                    vf += hwUploadSuffix;
                log("Creating intro...");
                Step("Encoding intro to MPEG-TS…");

                var introVidTail = new List<FfmpegOption?>();
                if (resolveSafeEncoding)
                    DaVinciOutputEncoding.AppendVideoEncodeOptions(introVidTail, hwAccelForIntro, encodeTenBit);
                else
                {
                    // hwupload produces VAAPI/AMF surfaces; source codec name (e.g. "hevc") maps to libx265 and fails.
                    var introCodec = DaVinciOutputEncoding.UsesHardwareAcceleration(hwAccelForIntro)
                        ? DaVinciOutputEncoding.GetHevcVideoEncoder(hwAccelForIntro)
                        : vCodec;
                    introVidTail.Add(FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, introCodec));
                }

                var introMp4Parts = new List<FfmpegOption?>();
                if (resolveSafeEncoding || (DaVinciOutputEncoding.UsesVaapi(hwAccelForIntro) && OperatingSystem.IsLinux()))
                    DaVinciOutputEncoding.AppendPreInputHwOptions(introMp4Parts, hwAccelForIntro);
                else if (DaVinciOutputEncoding.UsesHardwareAcceleration(hwAccelForIntro))
                    introMp4Parts.Add(FfmpegOption.Pair(FfmpegArguments.HardwareAcceleration, DaVinciOutputEncoding.GetHardwareAccelApi(hwAccelForIntro)));
                introMp4Parts.AddRange(
                    FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
                    FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
                    FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatLavfi),
                    FfmpegCommandLine.DefaultInputThreadQueue(),
                    FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted($"color=c=0x1E1E1E:s={w}x{h}:r={fps}:d={durationSeconds}")),
                    FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatLavfi),
                    FfmpegCommandLine.DefaultInputThreadQueue(),
                    FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted($"anullsrc=r={ar}:cl={acl}:d={durationSeconds}")),
                    FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{vf}\"")
                );
                introMp4Parts.AddRange(introVidTail);
                introMp4Parts.Add(FfmpegOption.Pair(FfmpegArguments.OutputVideoFrameRate, fps));
                // Only force AAC for Opus when building trim-style intermediates; combine keeps the matched encoder.
                if (useTrimSegmentAudioCodec && MpegTsConcatAudio.ShouldTranscodeAudioMp4ToTs(introAudioEnc))
                {
                    log("Intro audio is Opus: using AAC in MPEG-TS intermediate (avoids opus packet header errors when concatenating).");
                    introMp4Parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, "aac"));
                    introMp4Parts.Add(FfmpegOption.Pair(FfmpegArguments.AudioBitrate, "192k"));
                }
                else
                {
                    introMp4Parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, introAudioEnc));
                    if (!string.Equals(introAudioEnc, FfmpegArguments.StreamCopy, StringComparison.OrdinalIgnoreCase))
                        introMp4Parts.Add(FfmpegOption.Pair(FfmpegArguments.AudioBitrate, "192k"));
                }

                if (resolveSafeEncoding)
                    DaVinciOutputEncoding.AppendPostInputHwOptions(introMp4Parts, hwAccelForIntro);

                introMp4Parts.Add(FfmpegOption.Pair(FfmpegArguments.AudioSampleRate, ar));
                introMp4Parts.Add(FfmpegOption.Pair(FfmpegArguments.AudioChannels, ach));
                introMp4Parts.Add(FfmpegOption.Unary(FfmpegArguments.StopEncodingWhenShortestStreamEnds));
                // Intro is HEVC when Resolve-safe / HW accel; otherwise matches source codec (e.g. AV1).
                var introEncodedCodec = resolveSafeEncoding || DaVinciOutputEncoding.UsesHardwareAcceleration(hwAccelForIntro)
                    ? "hevc"
                    : vCodec;
                introMp4Parts.Add(MpegTsVideoBitstream.GetMp4ToAnnexBOption(introEncodedCodec));
                introMp4Parts.Add(FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatMpegTs));
                introMp4Parts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(introTs)));

                await FFMpegUtils.Instance.RunCaptureFFMpegAsync(FfmpegCommandLine.Build(introMp4Parts), ct, log);
                progress?.Report(0.8);

                Step("Muxing main clip to MPEG-TS…");
                var inputToTs = new List<FfmpegOption?>
                {
                    FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
                    FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
                    seekBeforeMainInput,
                    FfmpegCommandLine.DefaultInputThreadQueue(),
                    FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(inputPath)),
                };
                if (resolveSafeEncoding)
                {
                    DaVinciOutputEncoding.AppendPreInputHwOptions(inputToTs, hwAccelForIntro);
                    var mainVfOpt = DaVinciOutputEncoding.BuildVideoFilterOption(drawTextFilter: null, hwAccelForIntro, encodeTenBit: encodeTenBit);
                    if (!mainVfOpt.IsSkipped)
                        inputToTs.Add(mainVfOpt);
                    DaVinciOutputEncoding.AppendPostInputHwOptions(inputToTs, hwAccelForIntro);
                    DaVinciOutputEncoding.AppendVideoEncodeOptions(inputToTs, hwAccelForIntro, encodeTenBit);
                }
                else
                {
                    inputToTs.Add(FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, FfmpegArguments.StreamCopy));
                    inputToTs.Add(MpegTsVideoBitstream.GetMp4ToAnnexBOption(vCodec));
                }

                // Combine: always copy main audio. Trim: may re-encode Opus→AAC for MPEG-TS.
                if (useTrimSegmentAudioCodec)
                {
                    if (MpegTsConcatAudio.ShouldTranscodeAudioMp4ToTs(aCodec))
                        log("Main clip audio is Opus: using AAC in MPEG-TS intermediate (avoids opus packet header errors when concatenating).");
                    MpegTsConcatAudio.AppendMp4ToTsAudioOptions(inputToTs, aCodec);
                    if (resolveSafeEncoding && MpegTsConcatAudio.ShouldTranscodeAudioMp4ToTs(aCodec))
                        inputToTs.Add(DaVinciOutputEncoding.BuildAudioTimestampResetOption());
                }
                else
                {
                    log("Combine intro merge: copying main clip audio.");
                    inputToTs.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, FfmpegArguments.StreamCopy));
                }

                if (resolveSafeEncoding)
                    inputToTs.Add(MpegTsVideoBitstream.GetMp4ToAnnexBOption("hevc"));
                inputToTs.Add(FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatMpegTs));
                inputToTs.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(inputTs)));
                await FFMpegUtils.Instance.RunCaptureFFMpegAsync(FfmpegCommandLine.Build(inputToTs), ct, log);
                progress?.Report(0.9);

                if (deleteSourceAfterRemux)
                    TryDeleteSourceAfterTempsReady(inputPath, outputPath, introTs, inputTs, log, Step);

                log("Concatenating...");
                Step("Joining intro and main clip…");
                var introAudioInTs = useTrimSegmentAudioCodec && MpegTsConcatAudio.ShouldTranscodeAudioMp4ToTs(introAudioEnc)
                    ? "aac"
                    : introAudioEnc;
                var mainAudioInTs = useTrimSegmentAudioCodec && MpegTsConcatAudio.ShouldTranscodeAudioMp4ToTs(aCodec)
                    ? "aac"
                    : aCodec;
                var needAacAdtsBsf = MpegTsConcatAudio.IntermediateTsAudioIsAac(introAudioInTs)
                    && MpegTsConcatAudio.IntermediateTsAudioIsAac(mainAudioInTs);
                var aacBsfOpt = needAacAdtsBsf
                    ? FfmpegOption.Pair(FfmpegArguments.AudioBitstreamFilter, FfmpegArguments.BitstreamFilterAacAdtsToAsc)
                    : (FfmpegOption?)null;
                var finalParts = new List<FfmpegOption?>
                {
                    FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
                    FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
                    FfmpegCommandLine.DefaultInputThreadQueue(),
                    FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted($"concat:{introTs}|{inputTs}")),
                    FfmpegOption.Pair(FfmpegArguments.SelectCodec, FfmpegArguments.StreamCopy),
                    aacBsfOpt,
                };
                if (resolveSafeEncoding)
                    DaVinciOutputEncoding.AppendMp4OutputOptions(finalParts);
                finalParts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(outputPath)));
                await FFMpegUtils.Instance.RunCaptureFFMpegAsync(
                    FfmpegCommandLine.Build(finalParts),
                    ct, log);
                progress?.Report(1.0);

                log("Done.");
            }
            finally
            {
                SafeDelete(introTs);
                SafeDelete(inputTs);
            }
        }

        /// <summary>
        /// After intro + main MPEG-TS intermediates exist, free the source file so the final
        /// concat has room on the same volume. Skips when source and output are the same path.
        /// </summary>
        private static void TryDeleteSourceAfterTempsReady(
            string inputPath,
            string outputPath,
            string introTs,
            string inputTs,
            Action<string> log,
            Action<string> step)
        {
            if (!File.Exists(introTs) || new FileInfo(introTs).Length == 0
                || !File.Exists(inputTs) || new FileInfo(inputTs).Length == 0)
            {
                log("Skipping source delete: MPEG-TS intermediates are missing or empty.");
                return;
            }

            try
            {
                var srcFull = Path.GetFullPath(inputPath);
                var outFull = Path.GetFullPath(outputPath);
                if (string.Equals(srcFull, outFull, StringComparison.OrdinalIgnoreCase))
                {
                    log("Skipping source delete: output path is the same as the source.");
                    return;
                }
            }
            catch (Exception ex)
            {
                log($"Skipping source delete: could not resolve paths ({ex.Message}).");
                return;
            }

            step("Deleting source to free space for output…");
            log($"Deleting source after remux to free space: {inputPath}");
            FileUtils.TryDeleteFile(inputPath);
            if (File.Exists(inputPath))
                log("Warning: source file could not be deleted; continuing with concat.");
            else
                log("Source deleted.");
        }

        private static string EscapeDrawtext(string s) =>
            (s ?? string.Empty).Replace("\\", "\\\\").Replace(":", "\\:").Replace("'", "\\'");

        private static void SafeDelete(string path)
        {
            TempPathHelper.DeleteTemporaryFileUnlessRetained(path);
        }
    }
}