using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Linux.ViewModels;

namespace VRCFaceTracking.Linux.Views;

public partial class MainWindow : Window
{
    private readonly HomePage _homePage;
    private readonly RegistryPage _registryPage;
    private readonly CalibrationPage _calibrationPage;
    private readonly ModulesPage _modulesPage;
    private readonly OutputPage _outputPage;
    private readonly SettingsPage _settingsPage;
    private readonly RegistryViewModel _registryVm;
    private bool _shuttingDown;

    public MainWindow(MainWindowViewModel vm, RegistryViewModel registryVm, CalibrationViewModel calibrationVm)
    {
        InitializeComponent();
        DataContext = vm;
        _registryVm = registryVm;

        _homePage = new HomePage { DataContext = vm };
        _registryPage = new RegistryPage { DataContext = registryVm };
        _calibrationPage = new CalibrationPage { DataContext = calibrationVm };
        _modulesPage = new ModulesPage { DataContext = vm };
        _outputPage = new OutputPage { DataContext = vm };
        _settingsPage = new SettingsPage { DataContext = vm };

        ContentArea.Content = _homePage;
        HighlightNav(BtnHome);
    }

    // Stop the host while the Avalonia dispatcher is still running so that
    // Tmds.DBus (Wayland backend) can disconnect cleanly without hitting a
    // cancelled dispatcher and crashing the process.
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_shuttingDown)
        {
            e.Cancel = true;
            _ = ShutdownAsync();
        }
        base.OnClosing(e);
    }

    private async Task ShutdownAsync()
    {
        _shuttingDown = true;
        var services = App.Services!;
        var mainService = services.GetRequiredService<IMainService>();
        var host = services.GetRequiredService<IHost>();
        await mainService.Teardown();
        await host.StopAsync();
        Close();
    }

    private Button? _activeNav;
    private void HighlightNav(Button btn)
    {
        if (_activeNav != null)
            _activeNav.Classes.Remove("nav-active");
        _activeNav = btn;
        btn.Classes.Add("nav-active");
    }

    private void NavHome_Click(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = _homePage;
        HighlightNav(BtnHome);
    }
    private void NavRegistry_Click(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = _registryPage;
        HighlightNav(BtnRegistry);
        _ = _registryVm.LoadAsync();
    }
    private void NavCalibration_Click(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = _calibrationPage;
        HighlightNav(BtnCalibration);
    }
    private void NavModules_Click(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = _modulesPage;
        HighlightNav(BtnModules);
    }
    private void NavOutput_Click(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = _outputPage;
        HighlightNav(BtnOutput);
    }
    private void NavSettings_Click(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = _settingsPage;
        HighlightNav(BtnSettings);
    }
}
