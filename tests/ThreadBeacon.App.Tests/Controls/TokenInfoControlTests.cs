using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ThreadBeacon.App.Controls;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Tests.Controls;

public sealed class TokenInfoControlTests
{
    [Fact]
    public void Construct_WithTokenDetails_LoadsWithoutCompetingTooltip()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var application = new Application();
                application.Resources["SecondaryTextBrush"] = Brushes.DimGray;
                application.Resources["SurfaceBrush"] = Brushes.White;
                application.Resources["ControlBorderBrush"] = Brushes.LightGray;

                var control = new TokenInfoControl
                {
                    Details = new TokenDetailViewModel(
                        new TokenUsageSnapshot(100, null, null, null)),
                };

                control.Measure(new Size(16, 16));
                control.ApplyTemplate();

                var button = Assert.IsType<Button>(control.FindName("InfoButton"));
                var popup = Assert.IsType<Popup>(control.FindName("DetailsPopup"));
                Assert.Null(button.ToolTip);
                Assert.False(popup.StaysOpen);
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
