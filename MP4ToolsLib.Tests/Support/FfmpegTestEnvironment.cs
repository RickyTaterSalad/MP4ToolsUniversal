using System;
using MP4ToolsLib;

namespace MP4ToolsLib.Tests.Support;

/// <summary>Restores <see cref="FFMpegUtils.DryRunExternalCommands"/> and encoder catalog between tests.</summary>
public sealed class FfmpegTestEnvironment : IDisposable
{
	private readonly bool _previousDryRun;

	public FfmpegTestEnvironment(bool dryRunExternalCommands)
	{
		_previousDryRun = FFMpegUtils.DryRunExternalCommands;
		FFMpegUtils.DryRunExternalCommands = dryRunExternalCommands;
		FfmpegEncoderCatalog.InvalidateCache();
	}

	public void Dispose()
	{
		FFMpegUtils.DryRunExternalCommands = _previousDryRun;
		FfmpegEncoderCatalog.InvalidateCache();
	}
}
