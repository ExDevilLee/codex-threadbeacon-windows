using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ThreadBeacon.App.Controls;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Tests.Controls;

[Collection(WpfControlTestCollection.Name)]
public sealed class DataSourceHealthControlTests
{
    [Fact]
    public void Construct_UsesDismissiblePopupAndKeepsItOpenAcrossReportUpdates()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                Application application = Application.Current ?? new Application();
                application.Resources["SecondaryTextBrush"] = Brushes.DimGray;
                application.Resources["SurfaceBrush"] = Brushes.White;
                application.Resources["ControlBorderBrush"] = Brushes.LightGray;
                application.Resources["PrimaryTextBrush"] = Brushes.Black;
                application.Resources["BooleanToVisibilityConverter"] =
                    new BooleanToVisibilityConverter();

                var details = new DataSourceHealthViewModel();
                var control = new DataSourceHealthControl { Details = details };
                var window = new Window
                {
                    Width = 120,
                    Height = 80,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                    Content = control,
                };
                window.Show();

                try
                {
                    control.ApplyTemplate();
                    var button = Assert.IsType<Button>(control.FindName("HealthButton"));
                    var popup = Assert.IsType<Popup>(control.FindName("DetailsPopup"));
                    Assert.Equal(details.AccessibilityLabel, button.ToolTip);
                    Assert.False(popup.StaysOpen);

                    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.True(popup.IsOpen);
                    details.Update(new DataSourceHealthReport(
                        DataSourceHealthStatus.Healthy,
                        DataSourceHealthStatus.Healthy,
                        DataSourceHealthStatus.Healthy,
                        DataSourceHealthStatus.Healthy,
                        1,
                        0,
                        DateTimeOffset.Now));

                    Assert.True(popup.IsOpen);
                }
                finally
                {
                    window.Close();
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);

        thread.Start();
        thread.Join();

        Assert.Null(failure);
    }
}
