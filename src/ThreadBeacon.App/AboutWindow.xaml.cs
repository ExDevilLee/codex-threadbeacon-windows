using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace ThreadBeacon.App;

public partial class AboutWindow : Window
{
    public AboutWindow(ViewModels.UpdateCheckViewModel? updateCheck = null)
    {
        InitializeComponent();
        VersionText = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "unknown";
        UpdateCheck = updateCheck;
        DataContext = this;
    }

    public string VersionText { get; }

    public ViewModels.UpdateCheckViewModel? UpdateCheck { get; }

    private void OnLinkClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string uri })
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
