using System;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib.FFmpegArguments
{
    /// <summary>
    /// Extension methods to allow FFMpegUtils to work with FFmpegArgumentBuilder.
    /// </summary>
    public static class FFMpegUtilsExtensions
    {
        /// <summary>
        /// Runs ffmpeg with arguments built from an FFmpegArgumentBuilder.
        /// Builds the arguments, applies forced args, then passes to the standard async method.
        /// </summary>
        public static Task<string> RunCaptureFFMpegAsync(
            this FFMpegUtils utils,
            FFmpegArgumentBuilder builder,
            CancellationToken ct,
            Action<string> log = null)
        {
            var args = builder.Build();
            args = utils.ApplyForcedFfmpegArgs(args);
            return utils.RunCaptureFFMpegAsync(args, ct, log);
        }

        /// <summary>
        /// Runs ffmpeg synchronously with arguments built from an FFmpegArgumentBuilder.
        /// </summary>
        public static void RunAndLogFFMpeg(
            this FFMpegUtils utils,
            FFmpegArgumentBuilder builder,
            Action<System.Diagnostics.Process, string, string> processOutputAction,
            Action<string> log,
            string workingDirectory = "")
        {
            var args = builder.Build();
            utils.RunAndLogFFMpeg(args, processOutputAction, log, workingDirectory);
        }

        /// <summary>
        /// Runs ffmpeg synchronously with arguments built from an FFmpegArgumentBuilder (no process output callback).
        /// </summary>
        public static void RunAndLogFFMpeg(
            this FFMpegUtils utils,
            FFmpegArgumentBuilder builder,
            Action<string> log,
            string workingDirectory = "")
        {
            var args = builder.Build();
            utils.RunAndLogFFMpeg(args, null, log, workingDirectory);
        }

        /// <summary>
        /// Runs ffprobe with arguments built from an FFmpegArgumentBuilder.
        /// </summary>
        public static Task<string> RunCaptureFFProbeAsync(
            this FFMpegUtils utils,
            FFmpegArgumentBuilder builder,
            CancellationToken ct,
            Action<string> log = null)
        {
            var args = builder.Build();
            return utils.RunCaptureFFProbeAsync(args, ct, log);
        }
    }
}
