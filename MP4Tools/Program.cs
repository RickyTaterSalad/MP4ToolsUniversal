using Avalonia;
using System;
using System.IO;
using System.Runtime.Loader;

namespace MP4Tools;

sealed class Program
{
    // Must match ~/.local/share/applications/mp4tools.desktop (basename without .desktop).
    private const string LinuxDesktopAppId = "mp4tools";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // When MP4Tools.deps.json is stale, the runtime still refuses to load project refs even if
        // MP4ToolsLib.dll is next to MP4Tools.dll. Resolve from the app base as a fallback.
        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            if (!string.Equals(name.Name, "MP4ToolsLib", StringComparison.Ordinal))
                return null;
            var path = Path.Combine(AppContext.BaseDirectory, "MP4ToolsLib.dll");
            return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
        };

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Native Wayland with xdg_toplevel app_id so GNOME matches mp4tools.desktop;
            // falls back to X11/XWayland when Wayland is unavailable.
            .UseWaylandWithFallback()
            .With(new WaylandPlatformOptions { AppId = LinuxDesktopAppId })
            .With(new X11PlatformOptions { OverlayPopups = true, WmClass = "MP4Tools" })
            .With(new Win32PlatformOptions { OverlayPopups = true }) // Optional for cross-platform
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
