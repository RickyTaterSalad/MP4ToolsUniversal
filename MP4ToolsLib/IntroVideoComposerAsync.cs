using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib
{
    public static class IntroVideoComposerAsync
    {
        private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(20);

        private static string _font;
        private static string Font
        {
            get
            {
                if (_font == null)
                {
                    _font = "C:/Windows/Fonts/calibri.ttf";
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
            CancellationToken ct = default)
        {
            log ??= _ => { };
            if (!File.Exists(inputPath))
                throw new FileNotFoundException(inputPath);

            string introMp4 = Path.Combine(TempPathHelper.GetTempPath(), $"intro_{Guid.NewGuid():N}.mp4");
            string introTs = Path.Combine(TempPathHelper.GetTempPath(), $"intro_{Guid.NewGuid():N}.ts");
            string inputTs = Path.Combine(TempPathHelper.GetTempPath(), $"input_{Guid.NewGuid():N}.ts");

            try
            {
                var (video, audio) = await FFMpegUtils.Instance.ProbeMediaInfoAsync(inputPath, ct);

                string w = !string.IsNullOrWhiteSpace(video?.Width) ? video.Width : "1920";
                string h = !string.IsNullOrWhiteSpace(video?.Height) ? video.Height : "1080";
                string fps = !string.IsNullOrWhiteSpace(video?.FrameRate) ? video.FrameRate : "30000/1001";
                string pixFmt = !string.IsNullOrWhiteSpace(video?.PixelFormat) ? video.PixelFormat : "yuv420p";
                string vCodec = (!string.IsNullOrWhiteSpace(video?.CodecName) ? video.CodecName : "h264").ToLowerInvariant();

                string ar = !string.IsNullOrWhiteSpace(audio?.SampleRate) ? audio.SampleRate : "48000";
                string ach = !string.IsNullOrWhiteSpace(audio?.Channels) ? audio.Channels : "2";
                string acl = !string.IsNullOrWhiteSpace(audio?.ChannelLayout) ? audio.ChannelLayout : (ach == "1" ? "mono" : "stereo");
                string aCodec = (!string.IsNullOrWhiteSpace(audio?.CodecName) ? audio.CodecName : "aac").ToLowerInvariant();

                string vEnc = vCodec switch
                {
                    "hevc" => "libx265",
                    "mpeg4" => "mpeg4",
                    _ => "libx264"
                };

                string aEnc = aCodec switch
                {
                    "mp3" => "libmp3lame",
                    "ac3" => "ac3",
                    "opus" => "libopus",
                    _ => "aac"
                };
                string GetBsfForCodec(string codec) => codec switch
                {
                    "hevc" => "hevc_mp4toannexb",
                    "h264" or "avc" => "h264_mp4toannexb",
                    _ => string.Empty
                };
                string vBsf = GetBsfForCodec(vCodec);

                var escapedTitle = EscapeDrawtext(titleText);
                var escapedSubtitle = EscapeDrawtext(subtitleText);
                var escapedDetails = EscapeDrawtext(detailsText);
                titleFontSize = Math.Max(1, titleFontSize);
                subtitleFontSize = Math.Max(1, subtitleFontSize);
                detailsFontSize = Math.Max(1, detailsFontSize);
                const int lineGap = 36;
                var vf = $"drawtext=text='{escapedTitle}':fontfile='{Font}':fontcolor=white:fontsize={titleFontSize}:x=(w-text_w)/2:y=(h/2)-text_h-{lineGap / 2}";
                if (!string.IsNullOrWhiteSpace(escapedSubtitle))
                {
                    vf += $",drawtext=text='{escapedSubtitle}':fontfile='{Font}':fontcolor=white:fontsize={subtitleFontSize}:x=(w-text_w)/2:y=(h/2)+{lineGap / 2}";
                }
                if (!string.IsNullOrWhiteSpace(escapedDetails))
                {
                    vf += $",drawtext=text='{escapedDetails}':fontfile='{Font}':fontcolor=white:fontsize={detailsFontSize}:x=(w-text_w)/2:y=(h/2)+{lineGap / 2}+{subtitleFontSize}+{lineGap}";
                }

                log("Creating intro...");
                await FFMpegUtils.Instance.RunCaptureFFMpegAsync(
                    FfmpegCommandLine.Build(
                        FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
                        FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
                        FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatLavfi),
                        FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted($"color=c=0x1E1E1E:s={w}x{h}:r={fps}:d={durationSeconds}")),
                        FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatLavfi),
                        FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted($"anullsrc=r={ar}:cl={acl}:d={durationSeconds}")),
                        FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{vf}\""),
                        FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, vEnc),
                        FfmpegOption.Pair(FfmpegArguments.PixelFormat, pixFmt),
                        FfmpegOption.Pair(FfmpegArguments.OutputVideoFrameRate, fps),
                        FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, aEnc),
                        FfmpegOption.Pair(FfmpegArguments.AudioSampleRate, ar),
                        FfmpegOption.Pair(FfmpegArguments.AudioChannels, ach),
                        FfmpegOption.Unary(FfmpegArguments.StopEncodingWhenShortestStreamEnds),
                        FfmpegOption.Positional(FfmpegCommandLine.Quoted(introMp4))),
                    ct, log);

                log("Muxing TS streams...");
                await FFMpegUtils.Instance.RunCaptureFFMpegAsync(
                    FfmpegCommandLine.Build(
                        FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
                        FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
                        FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(introMp4)),
                        FfmpegOption.Pair(FfmpegArguments.SelectCodec, FfmpegArguments.StreamCopy),
                        FfmpegOption.Pair(FfmpegArguments.VideoBitstreamFilter, vBsf),
                        FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatMpegTs),
                        FfmpegOption.Positional(FfmpegCommandLine.Quoted(introTs))),
                    ct, log);
                progress?.Report(0.8);
                await FFMpegUtils.Instance.RunCaptureFFMpegAsync(
                    FfmpegCommandLine.Build(
                        FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
                        FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
                        FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(inputPath)),
                        FfmpegOption.Pair(FfmpegArguments.SelectCodec, FfmpegArguments.StreamCopy),
                        FfmpegOption.Pair(FfmpegArguments.VideoBitstreamFilter, vBsf),
                        FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatMpegTs),
                        FfmpegOption.Positional(FfmpegCommandLine.Quoted(inputTs))),
                    ct, log);
                progress?.Report(0.9);

                log("Concatenating...");
                var aacBsfOpt = aEnc.Equals("aac", StringComparison.OrdinalIgnoreCase)
                    ? FfmpegOption.Pair(FfmpegArguments.AudioBitstreamFilter, FfmpegArguments.BitstreamFilterAacAdtsToAsc)
                    : (FfmpegOption?)null;
                await FFMpegUtils.Instance.RunCaptureFFMpegAsync(
                    FfmpegCommandLine.Build(
                        FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
                        FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
                        FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted($"concat:{introTs}|{inputTs}")),
                        FfmpegOption.Pair(FfmpegArguments.SelectCodec, FfmpegArguments.StreamCopy),
                        aacBsfOpt,
                        FfmpegOption.Positional(FfmpegCommandLine.Quoted(outputPath))),
                    ct, log);
                progress?.Report(1.0);

                log("Done.");
            }
            finally
            {
                SafeDelete(introMp4);
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