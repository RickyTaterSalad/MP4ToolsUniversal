using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MP4Tools.Services;
using MP4Tools.ViewModels;
using MP4Tools.Views;
using MP4ToolsLib;

namespace MP4Tools;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                FFMpegUtils.Instance.KillAllTrackedMediaProcesses();
            }
            catch
            {
                // ignored
            }
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var startupSettings = AppSettingsStore.LoadAndApply();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(startupSettings),
            };
            desktop.MainWindow.Closing += (_, _) =>
            {
                try
                {
                    FFMpegUtils.Instance.KillAllTrackedMediaProcesses();
                }
                catch
                {
                    // ignored
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}