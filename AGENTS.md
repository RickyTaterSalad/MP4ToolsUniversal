# Repository Guidelines

Contributor-facing notes for **MP4ToolsUniversal**: an Avalonia desktop app (.NET 10) that drives FFmpeg/ffprobe via **MP4ToolsLib** for trim, combine, intro overlays, and optional re-encode.

## Layout

| Area | Role |
|------|------|
| **MP4ToolsLib/** | FFmpeg/ffprobe wrappers (`FFMpegUtils`), CLI helpers (`FfmpegCli`, `FfmpegCommandLine`), encode planning (`VideoEncodeSelector`, `FfmpegEncoderCatalog`), intro pipeline (`IntroVideoComposerAsync`), combine metadata (`CombineFile`), Opus-safe TS mux helper (`MpegTsConcatAudio`), DTOs (`EncodingSettingsDto`). |
| **MP4ToolsLib.Tests/** | xUnit tests: hardware encode planning (dry-run encoder catalog), optional FFmpeg integration tests (see env vars below). |
| **MP4Tools/** | UI (Views), MVVM ViewModels, `Program.cs`, static `Logger`, Services (`AppSettingsStore`, `TempPathHelper`, `EncodingSettingsRuntime`, `DefaultOutputPathRuntime`). |

Settings persist under `%AppData%/MP4Tools/settings.json` (see `AppSettingsStore.SettingsFilePath`).

## Build & Run

- **Build**: `dotnet build`
- **Run**: `dotnet run` from `MP4Tools/`
- **Tests**: `dotnet test` — library tests live in **`MP4ToolsLib.Tests/`** (xUnit). Hardware-focused integration tests are skipped unless you set **`MP4TOOLS_TEST_TRIM_INPUT_DIR`** and **`MP4TOOLS_TEST_COMBINE_INPUT_DIR`** to folders of long (≥10 min default) `.mp4` files and **`ffmpeg`** / **`ffprobe`** are on `PATH`. Optional: **`MP4TOOLS_TEST_OUTPUT_DIR`**, **`MP4TOOLS_TEST_MIN_DURATION_SECONDS`** (default `600`). See **`TestMediaConfiguration`** in the test project.

Indent with **4 spaces**. Naming follows usual .NET / PascalCase for types.

## Commits & PRs

- Messages: `type(scope): description` — types include `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`.
- PRs: reference issues; include screenshots for visible UI changes.

---

## Encode preferences (shared by Trim & Combine)

The **Encode** tab persists **`EncodingSettingsDto`** inside **`AppUserSettings.Encoding`**. **`EncodingSettingsRuntime.Apply`** loads encoding prefs into **`EncodingSettingsRuntime.Current`** and also refreshes **`FfmpegUserHints.HardwareAcceleration`** (used when injecting decode hints via **`FFMpegUtils.ApplyForcedFfmpegArgs`**).

When video is **re-encoded** (not stream copy), **`VideoEncodeSelector.BuildPlan`** picks encoder FFmpeg CLI tails from persisted prefs plus **`InputVideoBitDepth`** (probe-derived UI hint on the base VM).

**Audio codec** for transcode steps uses **`VideoEncodeSelector.EffectiveAudioCodec`** (e.g. `copy`, `aac`, `libopus`). **`aac_adtstoasc`** is applied only when the effective audio path is AAC-related — never for Opus-only pipelines.

---

## Hardware acceleration (this codebase)

This fork targets **AMD on Linux via VA-API** for GPU-minded encode/decode hints:

- Encode tab choices are **`auto`**, **`vaapi`**, **`software`** (NVENC / QSV / VideoToolbox paths are disabled).
- **`FFMpegUtils.ResolveHwAccelLineFromUserHints`** maps the tab selection to `-hwaccel` / `-vaapi_device` style injection when args pass through **`ApplyForcedFfmpegArgs`**.
- **`FfmpegEncoderCatalog`** caches **`ffmpeg -encoders`**; **`DryRunExternalCommands`** skips running that subprocess — **`InvalidateCache()`** runs when dry-run toggles so encoder detection refreshes after a real settings save.

---

## Paths & settings

- **`TempDirectory`** / **`RetainTemporaryFiles`** → **`TempPathHelper`** for intermediates.
- **`DefaultOutputDirectory`** → **`DefaultOutputPathRuntime.Directory`**; empty string in JSON resolves to **`DefaultOutputPathRuntime.BuiltinFallbackDirectory`** (`/opt/Encodes`). Combine **`OutputPath`** and Trim export folders build under this default unless overridden.
- **`DryRunFfmpegCommands`** → **`FFMpegUtils.DryRunExternalCommands`**: **`RunCaptureAsync`** and **`RunAndLogFFMpegAsync`** log full exe + args (after **`ApplyForcedFfmpegArgs`** where applicable) and skip **`Process.Start`**.

Startup (**`App.axaml.cs`**) loads JSON then **`TempPathHelper`** + **`EncodingSettingsRuntime.Apply`** (which applies dry-run + default output dir).

---

## UI threading & responsiveness

### Long-running work

**Combine** and **Trim** run heavy work inside **`Task.Run`** so the UI thread does not block. **`Combine`** additionally marshals **`CanCombine`** toggles and **`OutputPath`** mutations onto the UI dispatcher (`Combine()` vs **`CombineInternal`**) so Avalonia-bound observables are not updated from random threadpool threads.

### Log pane

**`LogViewModel`** queues lines from **`Logger.LogMessageReceived`** into a **`ConcurrentQueue`**, waits ~50ms, then drains batches **off** the UI delay continuation with **`ConfigureAwait(false)`** and applies a single **`Dispatcher.UIThread.InvokeAsync`** per batch. Each batch appends to one **`StringBuilder`** and assigns **`LogText`** once (read-only **`TextBox`**, **`Mode=OneWay`**) — avoid **`ObservableCollection`**/`Join` per batch for massive FFmpeg stderr bursts.

Repeated FFmpeg progress lines (`frame=` / `fps=`) are throttled (every Nth duplicate skipped).

### In-tab progress text

**`CombineViewModel.OperationStatus`** and **`TrimViewModel.OperationStatus`** show high-level steps (and terminal **Finished successfully.** / **Failed:** / **Cancelled.** / **Stopped:**). Updates use **`Dispatcher.UIThread.Post`** when callers run off the UI thread.

---

## MPEG-TS concat & Opus

Copying **Opus** into **MPEG-TS** then **`concat:`**-joining streams commonly triggers FFmpeg **`error parsing opus packet header`** spam / flaky mux.

**Mitigation**: **`MpegTsConcatAudio`** — when probing suggests Opus, MP4→TS intermediate steps use **AAC transcode** for audio (**`-c:a aac -b:a 192k`**) while keeping video copy + Annex B BSFs as today. **`GetFirstAudioCodecNameAsync`** (ffprobe) drives that decision on Trim multi-segment and Combine incremental-merge paths; **`IntroVideoComposerAsync`** does similar using encoder labels.

Final TS→MP4 steps enable **`aac_adtstoasc`** when mux logic detects AAC in TS (including Opus→AAC intermediates), not only when the Encode tab string literally contains `"aac"`.

---

## Intro composition

**`IntroVideoComposerAsync.PrependIntroAsync`** builds an intro from lavfi **`color`** plus **`drawtext`**, muxes to TS with Annex B video BSF, concatenates with main content, then muxes out. Encode-tab prefs can steer intro encode via **`VideoEncodeSelector.BuildIntroPlan`** (VA-API upload chain avoided on the intro path via **`forbidVaapi`**).

---

## Bit depth

Input probe surfaces **`InputVideoBitDepth`** on the shared VM base; Encode tab **`OutputBitDepth`** participates in **`VideoEncodeSelector`** when re-encoding. Stream copy preserves source bit depth; software encode paths use **`yuv420p` / `yuv420p10le`** as configured.

---

## FFmpeg entry points

- **Logged interactive encode**: **`MP4ViewModelBase.RunAndLogFFMpegAsync`** → **`FFMpegUtils.RunAndLogFFMpegAsync`** (prepends **`ApplyForcedFfmpegArgs`** when not stream-copy).
- **Captured output**: **`RunCaptureAsync`** / **`RunCaptureFFMpegAsync`** / **`RunCaptureFFProbeAsync`** (intro steps, probes, durations — dry-run logs without executing).

---

## Pitfalls checklist

1. **Codec-specific BSFs** — `aac_adtstoasc` only for AAC in context; video Annex B filters only for H.264/HEVC when remuxing MP4→TS.
2. **Thread affinity** — observable UI properties and Avalonia **`DispatcherOperation`** await semantics (avoid wrong **`ConfigureAwait`** on dispatcher ops).
3. **Dry-run** — encoder catalog empty until dry-run is off and cache invalidated; combine/trim outputs will not exist while dry-run is on.
4. **Default output folder** — combine **`combined.mp4`** for folder inputs nests under **`{DefaultDir}/{folderName}/`** to reduce collisions when multiple jobs share one encode root.

When extending FFmpeg flows, keep **`MP4ToolsLib`** free of direct **`AppSettingsStore`** references — pass behavior via static flags (**`FFMpegUtils.DryRunExternalCommands`**) or parameters set from **`EncodingSettingsRuntime`** / **`DefaultOutputPathRuntime`**.
