using System;
using OpenCvSharp;

namespace MP4ToolsLib;

/// <summary>
/// Detects Combine / Replace-Segment intro cards: solid background with text only
/// (typically 3–10 seconds, usually at the start of the video).
/// </summary>
public static class IntroTitleCardClassifier
{
	/// <summary>
	/// Returns true when the frame looks like a generated title card rather than live field footage.
	/// </summary>
	public static bool IsSolidTextCard(Mat bgr)
	{
		if (bgr == null || bgr.Empty())
			return false;

		using var small = new Mat();
		var scale = Math.Min(1.0, 240.0 / Math.Max(1, bgr.Width));
		var w = Math.Max(16, (int)Math.Round(bgr.Width * scale));
		var h = Math.Max(16, (int)Math.Round(bgr.Height * scale));
		Cv2.Resize(bgr, small, new Size(w, h), 0, 0, InterpolationFlags.Area);

		using var blur = new Mat();
		Cv2.GaussianBlur(small, blur, new Size(3, 3), 0);

		// Dominant near-median color coverage (solid background).
		var rows = blur.Rows;
		var cols = blur.Cols;
		var bVals = new int[rows * cols];
		var gVals = new int[bVals.Length];
		var rVals = new int[bVals.Length];
		var n = 0;
		for (var y = 0; y < rows; y++)
		{
			for (var x = 0; x < cols; x++)
			{
				var px = blur.At<Vec3b>(y, x);
				bVals[n] = px.Item0;
				gVals[n] = px.Item1;
				rVals[n] = px.Item2;
				n++;
			}
		}

		Array.Sort(bVals, 0, n);
		Array.Sort(gVals, 0, n);
		Array.Sort(rVals, 0, n);
		var mid = n / 2;
		var mb = bVals[mid];
		var mg = gVals[mid];
		var mr = rVals[mid];

		var nearBg = 0;
		var farText = 0;
		long colorDiffSum = 0;
		for (var y = 0; y < rows; y++)
		{
			for (var x = 0; x < cols; x++)
			{
				var px = blur.At<Vec3b>(y, x);
				var db = Math.Abs(px.Item0 - mb);
				var dg = Math.Abs(px.Item1 - mg);
				var dr = Math.Abs(px.Item2 - mr);
				var dist = db + dg + dr;
				colorDiffSum += dist;
				if (dist <= 36)
					nearBg++;
				else if (dist >= 90)
					farText++;
			}
		}

		var nearRatio = nearBg / (double)n;
		var farRatio = farText / (double)n;
		var meanAbsDiff = colorDiffSum / (double)n;

		// Field footage usually has much more mid-tone variation than a solid card.
		if (nearRatio < 0.88)
			return false;
		// Need some contrasting text/graphics, but not a busy scene.
		if (farRatio < 0.002 || farRatio > 0.18)
			return false;
		if (meanAbsDiff > 28)
			return false;

		// Texture: title cards are smooth except for glyph edges.
		using var gray = new Mat();
		Cv2.CvtColor(blur, gray, ColorConversionCodes.BGR2GRAY);
		using var lap = new Mat();
		Cv2.Laplacian(gray, lap, MatType.CV_64F);
		Cv2.MeanStdDev(lap, out _, out var lapStd);
		var texture = lapStd.Val0;
		// Smooth solid+text is typically low-moderate; grass/crowd is higher.
		if (texture > 18.0)
			return false;

		return true;
	}
}
