using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace MP4ToolsLib;

public readonly record struct DetectedPerson(Rect Box, float Confidence);

/// <summary>
/// YOLOv8 ONNX person detector (COCO class 0) running on the CPU execution provider.
/// </summary>
public sealed class YoloPersonDetector : IDisposable
{
	private const int InputSize = 640;
	private const int PersonClassId = 0;

	private readonly InferenceSession _session;
	private readonly string _inputName;
	private readonly float _confidenceThreshold;
	private readonly float _iouThreshold;

	public YoloPersonDetector(string modelPath, float confidenceThreshold = 0.35f, float iouThreshold = 0.45f)
	{
		if (string.IsNullOrWhiteSpace(modelPath))
			throw new ArgumentException("Model path is required.", nameof(modelPath));

		_confidenceThreshold = confidenceThreshold;
		_iouThreshold = iouThreshold;

		var options = new SessionOptions
		{
			GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
			IntraOpNumThreads = Math.Clamp(Environment.ProcessorCount / 2, 2, 8),
			InterOpNumThreads = 1,
		};
		_session = new InferenceSession(modelPath, options);
		_inputName = _session.InputMetadata.Keys.First();
	}

	public IReadOnlyList<DetectedPerson> Detect(Mat bgrFrame)
	{
		if (bgrFrame == null || bgrFrame.Empty())
			return Array.Empty<DetectedPerson>();

		var origW = bgrFrame.Width;
		var origH = bgrFrame.Height;
		var (tensor, scale, padX, padY) = LetterboxToTensor(bgrFrame);

		using var results = _session.Run(new[]
		{
			NamedOnnxValue.CreateFromTensor(_inputName, tensor),
		});

		var output = results[0].AsEnumerable<float>().ToArray();
		var dims = results[0].AsTensor<float>().Dimensions.ToArray();
		return ParseYoloV8(output, dims, origW, origH, scale, padX, padY);
	}

	private List<DetectedPerson> ParseYoloV8(
		float[] output,
		int[] dims,
		int origW,
		int origH,
		float scale,
		float padX,
		float padY)
	{
		// Expected: [1, 84, N] or [1, N, 84]
		int channels;
		int anchors;
		bool channelsFirst;
		if (dims.Length == 3 && dims[1] < dims[2])
		{
			channels = dims[1];
			anchors = dims[2];
			channelsFirst = true;
		}
		else if (dims.Length == 3)
		{
			anchors = dims[1];
			channels = dims[2];
			channelsFirst = false;
		}
		else if (dims.Length == 2)
		{
			// [84, N] or [N, 84]
			if (dims[0] < dims[1])
			{
				channels = dims[0];
				anchors = dims[1];
				channelsFirst = true;
			}
			else
			{
				anchors = dims[0];
				channels = dims[1];
				channelsFirst = false;
			}
		}
		else
		{
			return new List<DetectedPerson>();
		}

		var classCount = channels - 4;
		if (classCount <= PersonClassId)
			return new List<DetectedPerson>();

		var boxes = new List<Rect>();
		var scores = new List<float>();

		for (var i = 0; i < anchors; i++)
		{
			float cx, cy, w, h, personScore;
			if (channelsFirst)
			{
				cx = output[0 * anchors + i];
				cy = output[1 * anchors + i];
				w = output[2 * anchors + i];
				h = output[3 * anchors + i];
				personScore = output[(4 + PersonClassId) * anchors + i];
			}
			else
			{
				var baseIdx = i * channels;
				cx = output[baseIdx + 0];
				cy = output[baseIdx + 1];
				w = output[baseIdx + 2];
				h = output[baseIdx + 3];
				personScore = output[baseIdx + 4 + PersonClassId];
			}

			if (personScore < _confidenceThreshold)
				continue;

			// Undo letterbox
			var x1 = (cx - w / 2f - padX) / scale;
			var y1 = (cy - h / 2f - padY) / scale;
			var x2 = (cx + w / 2f - padX) / scale;
			var y2 = (cy + h / 2f - padY) / scale;

			var left = (int)Math.Clamp(Math.Floor(x1), 0, origW - 1);
			var top = (int)Math.Clamp(Math.Floor(y1), 0, origH - 1);
			var right = (int)Math.Clamp(Math.Ceiling(x2), 0, origW - 1);
			var bottom = (int)Math.Clamp(Math.Ceiling(y2), 0, origH - 1);
			var bw = Math.Max(1, right - left);
			var bh = Math.Max(1, bottom - top);
			boxes.Add(new Rect(left, top, bw, bh));
			scores.Add(personScore);
		}

		if (boxes.Count == 0)
			return new List<DetectedPerson>();

		var indices = NonMaxSuppression(boxes, scores, _iouThreshold);
		var people = new List<DetectedPerson>(indices.Count);
		foreach (var idx in indices)
			people.Add(new DetectedPerson(boxes[idx], scores[idx]));
		return people;
	}

	private static List<int> NonMaxSuppression(IReadOnlyList<Rect> boxes, IReadOnlyList<float> scores, float iouThreshold)
	{
		var order = Enumerable.Range(0, boxes.Count)
			.OrderByDescending(i => scores[i])
			.ToList();
		var kept = new List<int>();

		while (order.Count > 0)
		{
			var best = order[0];
			kept.Add(best);
			order.RemoveAt(0);
			order.RemoveAll(i => IoU(boxes[best], boxes[i]) > iouThreshold);
		}

		return kept;
	}

	private static float IoU(Rect a, Rect b)
	{
		var x1 = Math.Max(a.X, b.X);
		var y1 = Math.Max(a.Y, b.Y);
		var x2 = Math.Min(a.X + a.Width, b.X + b.Width);
		var y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);
		var interW = Math.Max(0, x2 - x1);
		var interH = Math.Max(0, y2 - y1);
		var inter = interW * interH;
		if (inter <= 0)
			return 0;
		var union = a.Width * a.Height + b.Width * b.Height - inter;
		return union <= 0 ? 0 : (float)inter / union;
	}

	private static (DenseTensor<float> tensor, float scale, float padX, float padY) LetterboxToTensor(Mat bgr)
	{
		var scale = Math.Min((float)InputSize / bgr.Width, (float)InputSize / bgr.Height);
		var newW = (int)Math.Round(bgr.Width * scale);
		var newH = (int)Math.Round(bgr.Height * scale);
		var padX = (InputSize - newW) / 2f;
		var padY = (InputSize - newH) / 2f;

		using var resized = new Mat();
		Cv2.Resize(bgr, resized, new Size(newW, newH));

		using var padded = new Mat(new Size(InputSize, InputSize), MatType.CV_8UC3, new Scalar(114, 114, 114));
		var roi = new Rect((int)padX, (int)padY, newW, newH);
		using (var dest = new Mat(padded, roi))
			resized.CopyTo(dest);

		using var rgb = new Mat();
		Cv2.CvtColor(padded, rgb, ColorConversionCodes.BGR2RGB);

		var data = new float[1 * 3 * InputSize * InputSize];
		// CHW normalize /255
		for (var y = 0; y < InputSize; y++)
		{
			for (var x = 0; x < InputSize; x++)
			{
				var px = rgb.At<Vec3b>(y, x);
				var idx = y * InputSize + x;
				data[0 * InputSize * InputSize + idx] = px.Item0 / 255f;
				data[1 * InputSize * InputSize + idx] = px.Item1 / 255f;
				data[2 * InputSize * InputSize + idx] = px.Item2 / 255f;
			}
		}

		var tensor = new DenseTensor<float>(data, new[] { 1, 3, InputSize, InputSize });
		return (tensor, scale, padX, padY);
	}

	public void Dispose() => _session?.Dispose();
}
