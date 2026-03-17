using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using VRCFaceTracking.Linux.ViewModels;
using VRCFaceTracking.Linux.Views;

namespace VRCFaceTracking.Linux;

public class App : Application
{
    public static IServiceProvider? Services { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = Services!.GetRequiredService<MainWindowViewModel>();
            var registryVm = Services!.GetRequiredService<RegistryViewModel>();
            desktop.MainWindow = new MainWindow(vm, registryVm);
        }
        base.OnFrameworkInitializationCompleted();
    }
}
