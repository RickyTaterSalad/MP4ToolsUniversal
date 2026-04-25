using MP4ToolsLib;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
				if (OperatingSystem.IsLinux()){
					_font = "/usr/share/fonts/truetype/noto/NotoSansMono-Regular.ttf";
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
		if (!File.Exists(inputPath)) throw new FileNotFoundException(inputPath);

		string introMp4 = Path.Combine(Path.GetTempPath(), $"intro_{Guid.NewGuid():N}.mp4");
		string introTs = Path.Combine(Path.GetTempPath(), $"intro_{Guid.NewGuid():N}.ts");
		string inputTs = Path.Combine(Path.GetTempPath(), $"input_{Guid.NewGuid():N}.ts");

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

			string vEnc = vCodec switch { "hevc" => "libx265", "mpeg4" => "mpeg4", _ => "libx264" };
			string aEnc = aCodec switch { "mp3" => "libmp3lame", "ac3" => "ac3", "opus" => "libopus", _ => "aac" };
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
				$"-nostdin -y -f lavfi -i \"color=c=0x1E1E1E:s={w}x{h}:r={fps}:d={durationSeconds}\" " +
				$"-f lavfi -i \"anullsrc=r={ar}:cl={acl}:d={durationSeconds}\" " +
				$"-vf \"{vf}\" " +
				$"-c:v {vEnc} -pix_fmt {pixFmt} -r {fps} -c:a {aEnc} -ar {ar} -ac {ach} -shortest \"{introMp4}\"",
				ct);

			log("Muxing TS streams...");
			await FFMpegUtils.Instance.RunCaptureFFMpegAsync($"-nostdin -y -i \"{introMp4}\" -c copy -bsf:v {vBsf} -f mpegts \"{introTs}\"", ct, log);
			progress?.Report(0.8);
			await FFMpegUtils.Instance.RunCaptureFFMpegAsync($"-nostdin -y -i \"{inputPath}\" -c copy -bsf:v {vBsf} -f mpegts \"{inputTs}\"", ct, log);
			progress?.Report(0.9);

			log("Concatenating...");
			await FFMpegUtils.Instance.RunCaptureFFMpegAsync($"-nostdin -y -i \"concat:{introTs}|{inputTs}\" -c copy -bsf:a aac_adtstoasc \"{outputPath}\"", ct, log);
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
		try { if (File.Exists(path)) File.Delete(path); } catch { }
	}
}