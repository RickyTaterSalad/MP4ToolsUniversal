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
            bool resolveSafeEncoding = true
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

        private static string EscapeDrawtext(string s) =>
            (s ?? string.Empty).Replace("\\", "\\\\").Replace(":", "\\:").Replace("'", "\\'");

        private static void SafeDelete(string path)
        {
            TempPathHelper.DeleteTemporaryFileUnlessRetained(path);
        }
    }
}