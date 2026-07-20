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
            try
            {
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            }
            catch
            {
                MessageBox.Show(
                    this,
                    FindResource("OpenLinkFailedMessage") as string ?? "Unable to open the link.",
                    FindResource("OpenLinkFailedTitle") as string ?? "ThreadBeacon",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
