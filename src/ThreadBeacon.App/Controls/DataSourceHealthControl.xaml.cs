using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThreadBeacon.App.ViewModels;

namespace ThreadBeacon.App.Controls;

public partial class DataSourceHealthControl : UserControl
{
    public static readonly DependencyProperty DetailsProperty = DependencyProperty.Register(
        nameof(Details),
        typeof(DataSourceHealthViewModel),
        typeof(DataSourceHealthControl),
        new PropertyMetadata(null, OnDetailsChanged));

    public DataSourceHealthControl() => InitializeComponent();

    public DataSourceHealthViewModel? Details
    {
        get => (DataSourceHealthViewModel?)GetValue(DetailsProperty);
        set => SetValue(DetailsProperty, value);
    }

    private static void OnDetailsChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not DataSourceHealthControl control)
        {
            return;
        }

        control.PopupContent.DataContext = e.NewValue;
        if (e.NewValue is null)
        {
            control.DetailsPopup.IsOpen = false;
        }
    }

    private void OnHealthButtonClick(object sender, RoutedEventArgs e)
    {
        if (Details is not null)
        {
            DetailsPopup.IsOpen = !DetailsPopup.IsOpen;
        }
    }

    private void OnHealthButtonKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape && DetailsPopup.IsOpen)
        {
            DetailsPopup.IsOpen = false;
            e.Handled = true;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) =>
        DetailsPopup.IsOpen = false;
}
