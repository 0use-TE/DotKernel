using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DotKernel.AvaExample.Services;
using DotKernel.AvaExample.Views;
using Markdig;
using MarkView.Avalonia;

namespace DotKernel.AvaExample;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ConfigureMarkdown();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = AppBootstrap.CreateMainViewModel(useUserSecrets: true),
            };
            desktop.MainWindow = mainWindow;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView
            {
                DataContext = AppBootstrap.CreateMainViewModel(useUserSecrets: false),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureMarkdown()
    {
        MarkdownViewerDefaults.Pipeline = new MarkdownPipelineBuilder()
            .UseSupportedExtensions()
            .UseAlertBlocks()
            .Build();

        MarkdownViewer.LinkClickedEvent.AddClassHandler<MarkdownViewer>((_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Url))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(e.Url) { UseShellExecute = true });
            }
            catch
            {
                // Browser/WASM may not support shell execute.
            }
        });
    }
}
