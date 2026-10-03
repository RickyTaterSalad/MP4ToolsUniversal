using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace MP4ToolsLib;

public readonly record struct DetectedObject(Rect Box, float Confidence, int ClassId, string ClassName);

public enum YoloModelKind
{
	/// <summary>COCO YOLOv8n — person class only.</summary>
	PersonCoco,

	/// <summary>BaseballCV pitcher/hitter/catcher detector (ONNX export).</summary>
	BaseballPhc,
}

/// <summary>
/// YOLOv8-family ONNX detector (CPU). Supports COCO person and BaseballCV PHC class maps.
/// </summary>
public sealed class YoloOnnxDetector : IDisposable
{
	private const int InputSize = 640;

	private static readonly string[] PersonClassNames = { "person" };
	private static readonly string[] BaseballPhcClassNames = { "hitter", "pitcher", "catcher" };

	private readonly InferenceSession _session;
	private readonly string _inputName;
	private readonly float _confidenceThreshold;
	private readonly float _iouThreshold;
	private readonly string[] _classNames;
	private readonly HashSet<int> _allowedClassIds;

	public YoloModelKind Kind { get; }
	public IReadOnlyList<string> ClassNames => _classNames;

	public YoloOnnxDetector(
		string modelPath,
		YoloModelKind kind,
		float confidenceThreshold = 0.35f,
		float iouThreshold = 0.45f)
	{
		if (string.IsNullOrWhiteSpace(modelPath))
			throw new ArgumentException("Model path is required.", nameof(modelPath));

		Kind = kind;
		_confidenceThreshold = confidenceThreshold;
		_iouThreshold = iouThreshold;
		_classNames = kind switch
		{
			YoloModelKind.BaseballPhc => BaseballPhcClassNames,
			_ => PersonClassNames,
		};
		_allowedClassIds = kind == YoloModelKind.PersonCoco
			? new HashSet<int> { 0 }
			: new HashSet<int>(Enumerable.Range(0, _classNames.Length));

		var options = new SessionOptions
		{
			GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
			IntraOpNumThreads = Math.Clamp(Environment.ProcessorCount / 2, 2, 8),
			InterOpNumThreads = 1,
		};
		_session = new InferenceSession(modelPath, options);
		_inputName = _session.InputMetadata.Keys.First();
	}

	public IReadOnlyList<DetectedObject> Detect(Mat bgrFrame)
	{
		if (bgrFrame == null || bgrFrame.Empty())
			return Array.Empty<DetectedObject>();

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

	public bool HasClass(IReadOnlyList<DetectedObject> detections, string className) =>
		detections.Any(d => string.Equals(d.ClassName, className, StringComparison.OrdinalIgnoreCase));

	private List<DetectedObject> ParseYoloV8(
		float[] output,
		int[] dims,
		int origW,
		int origH,
		float scale,
		float padX,
		float padY)
	{
		// Expected: [1, 4+nc, N] or [1, N, 4+nc]
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
			return new List<DetectedObject>();
		}

		var classCount = channels - 4;
		if (classCount <= 0)
			return new List<DetectedObject>();

		var boxes = new List<Rect>();
		var scores = new List<float>();
		var classIds = new List<int>();

		for (var i = 0; i < anchors; i++)
		{
			float cx, cy, w, h;
			if (channelsFirst)
			{
				cx = output[0 * anchors + i];
				cy = output[1 * anchors + i];
				w = output[2 * anchors + i];
				h = output[3 * anchors + i];
			}
			else
			{
				var baseIdx = i * channels;
				cx = output[baseIdx + 0];
				cy = output[baseIdx + 1];
				w = output[baseIdx + 2];
				h = output[baseIdx + 3];
			}

			var bestClass = -1;
			var bestScore = 0f;
			for (var c = 0; c < classCount; c++)
			{
				if (!_allowedClassIds.Contains(c))
					continue;

				float score;
				if (channelsFirst)
					score = output[(4 + c) * anchors + i];
				else
					score = output[i * channels + 4 + c];

				if (score > bestScore)
				{
					bestScore = score;
					bestClass = c;
				}
			}

			if (bestClass < 0 || bestScore < _confidenceThreshold)
				continue;

			var x1 = (cx - w / 2f - padX) / scale;
			var y1 = (cy - h / 2f - padY) / scale;
			var x2 = (cx + w / 2f - padX) / scale;
			var y2 = (cy + h / 2f - padY) / scale;

			var left = (int)Math.Clamp(Math.Floor(x1), 0, origW - 1);
			var top = (int)Math.Clamp(Math.Floor(y1), 0, origH - 1);
			var right = (int)Math.Clamp(Math.Ceiling(x2), 0, origW - 1);
			var bottom = (int)Math.Clamp(Math.Ceiling(y2), 0, origH - 1);
			boxes.Add(new Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top)));
			scores.Add(bestScore);
			classIds.Add(bestClass);
		}

		if (boxes.Count == 0)
			return new List<DetectedObject>();

		var indices = NonMaxSuppression(boxes, scores, _iouThreshold);
		var detections = new List<DetectedObject>(indices.Count);
		foreach (var idx in indices)
		{
			var classId = classIds[idx];
			var name = classId >= 0 && classId < _classNames.Length
				? _classNames[classId]
				: $"class_{classId}";
			detections.Add(new DetectedObject(boxes[idx], scores[idx], classId, name));
		}

		return detections;
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

/// <summary>Backward-compatible person-only wrapper over <see cref="YoloOnnxDetector"/>.</summary>
public readonly record struct DetectedPerson(Rect Box, float Confidence);

/// <summary>Backward-compatible alias for COCO person detection.</summary>
public sealed class YoloPersonDetector : IDisposable
{
	private readonly YoloOnnxDetector _inner;

	public YoloPersonDetector(string modelPath, float confidenceThreshold = 0.35f, float iouThreshold = 0.45f)
	{
		_inner = new YoloOnnxDetector(modelPath, YoloModelKind.PersonCoco, confidenceThreshold, iouThreshold);
	}

	public IReadOnlyList<DetectedPerson> Detect(Mat bgrFrame)
	{
		var objects = _inner.Detect(bgrFrame);
		var people = new List<DetectedPerson>(objects.Count);
		foreach (var o in objects)
			people.Add(new DetectedPerson(o.Box, o.Confidence));
		return people;
	}

	public void Dispose() => _inner.Dispose();
}
