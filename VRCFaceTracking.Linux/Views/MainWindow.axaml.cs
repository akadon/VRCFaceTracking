using Avalonia.Controls;
using Avalonia.Interactivity;
using VRCFaceTracking.Linux.ViewModels;

namespace VRCFaceTracking.Linux.Views;

public partial class MainWindow : Window
{
    private readonly HomePage _homePage;
    private readonly RegistryPage _registryPage;
    private readonly ModulesPage _modulesPage;
    private readonly OutputPage _outputPage;
    private readonly SettingsPage _settingsPage;
    private readonly RegistryViewModel _registryVm;

    public MainWindow(MainWindowViewModel vm, RegistryViewModel registryVm)
    {
        InitializeComponent();
        DataContext = vm;
        _registryVm = registryVm;

        _homePage = new HomePage { DataContext = vm };
        _registryPage = new RegistryPage { DataContext = registryVm };
        _modulesPage = new ModulesPage { DataContext = vm };
        _outputPage = new OutputPage { DataContext = vm };
        _settingsPage = new SettingsPage { DataContext = vm };

        ContentArea.Content = _homePage;
        HighlightNav(BtnHome);
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
