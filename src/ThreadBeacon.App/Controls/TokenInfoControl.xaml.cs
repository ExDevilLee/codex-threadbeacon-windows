using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ThreadBeacon.App.ViewModels;

namespace ThreadBeacon.App.Controls;

public partial class TokenInfoControl : UserControl
{
    public static readonly DependencyProperty DetailsProperty = DependencyProperty.Register(
        nameof(Details),
        typeof(TokenDetailViewModel),
        typeof(TokenInfoControl),
        new PropertyMetadata(null, OnDetailsChanged));

    private readonly DispatcherTimer openTimer;
    private readonly DispatcherTimer dismissTimer;
    private readonly TokenPopoverState state = new();
    private bool isApplyingState;

    public TokenInfoControl()
    {
        InitializeComponent();

        openTimer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(300),
        };
        openTimer.Tick += OnOpenTimerTick;

        dismissTimer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(150),
        };
        dismissTimer.Tick += OnDismissTimerTick;
    }

    public TokenDetailViewModel? Details
    {
        get => (TokenDetailViewModel?)GetValue(DetailsProperty);
        set => SetValue(DetailsProperty, value);
    }

    private static void OnDetailsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not TokenInfoControl control)
        {
            return;
        }

        control.PopupContent.DataContext = e.NewValue;
        if (e.NewValue is null)
        {
            control.ClosePopup();
        }
    }

    private void OnTriggerMouseEnter(object sender, MouseEventArgs e)
    {
        dismissTimer.Stop();
        if (!state.IsOpen && Details is not null)
        {
            openTimer.Stop();
            openTimer.Start();
        }
    }

    private void OnTriggerMouseLeave(object sender, MouseEventArgs e)
    {
        openTimer.Stop();
        ScheduleDismissal();
    }

    private void OnPopupMouseEnter(object sender, MouseEventArgs e)
    {
        openTimer.Stop();
        dismissTimer.Stop();
    }

    private void OnPopupMouseLeave(object sender, MouseEventArgs e) => ScheduleDismissal();

    private void OnInfoButtonClick(object sender, RoutedEventArgs e)
    {
        StopTimers();
        state.TogglePinned();
        ApplyState();
    }

    private void OnInfoButtonKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape && state.IsOpen)
        {
            ClosePopup();
            e.Handled = true;
        }
    }

    private void OnOpenTimerTick(object? sender, EventArgs e)
    {
        openTimer.Stop();
        if (Details is null)
        {
            return;
        }

        state.OpenForHover();
        ApplyState();
    }

    private void OnDismissTimerTick(object? sender, EventArgs e)
    {
        dismissTimer.Stop();
        state.RequestHoverDismiss();
        ApplyState();
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        if (isApplyingState)
        {
            return;
        }

        StopTimers();
        state.Close();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => ClosePopup();

    private void ScheduleDismissal()
    {
        dismissTimer.Stop();
        if (!state.IsPinned)
        {
            dismissTimer.Start();
        }
    }

    private void ClosePopup()
    {
        StopTimers();
        state.Close();
        ApplyState();
    }

    private void ApplyState()
    {
        isApplyingState = true;
        try
        {
            DetailsPopup.IsOpen = state.IsOpen && Details is not null;
        }
        finally
        {
            isApplyingState = false;
        }
    }

    private void StopTimers()
    {
        openTimer.Stop();
        dismissTimer.Stop();
    }
}
