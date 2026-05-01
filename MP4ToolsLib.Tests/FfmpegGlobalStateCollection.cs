using Xunit;

namespace MP4ToolsLib.Tests;

/// <summary>Serializes tests that touch static FFmpeg / encoder catalog flags.</summary>
[CollectionDefinition("FfmpegGlobalState", DisableParallelization = true)]
public sealed class FfmpegGlobalStateCollection
{
}
