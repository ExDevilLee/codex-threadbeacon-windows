using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ThreadBeacon.App.Controls;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Tests.Controls;

[Collection(WpfControlTestCollection.Name)]
public sealed class TokenInfoControlTests
{
    [Fact]
    public void Construct_InfoControls_LoadWithoutCompetingTooltipsAndDismissOutside()
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

                var subagentSnapshot = new SubagentSnapshot(
                    "child", "Child", ThreadStatus.Idle, DateTimeOffset.Now,
                    DateTimeOffset.Now, null, null, null, null, null, null,
                    RolloutSourceStatus.Healthy);
                var subagentControl = new SubagentInfoControl
                {
                    Details = new SubagentDetailViewModel(subagentSnapshot),
                };
                subagentControl.Measure(new Size(16, 16));
                subagentControl.ApplyTemplate();

                var subagentButton = Assert.IsType<Button>(subagentControl.FindName("InfoButton"));
                var subagentPopup = Assert.IsType<Popup>(subagentControl.FindName("DetailsPopup"));
                Assert.Null(subagentButton.ToolTip);
                Assert.False(subagentPopup.StaysOpen);
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
