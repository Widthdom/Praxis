using System.Windows.Input;

namespace Praxis.Behaviors;

public sealed class DragButtonBehavior : Behavior<View>
{
    public static readonly BindableProperty DragCommandProperty = BindableProperty.Create(
        nameof(DragCommand), typeof(ICommand), typeof(DragButtonBehavior));

    private readonly PanGestureRecognizer pan = new();
    private View? attachedView;
#if WINDOWS
    private Microsoft.UI.Xaml.FrameworkElement? nativeElement;
    private double totalX;
    private double totalY;
#endif

    public ICommand? DragCommand
    {
        get => (ICommand?)GetValue(DragCommandProperty);
        set => SetValue(DragCommandProperty, value);
    }

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        attachedView = bindable;
        pan.PanUpdated += OnPanUpdated;
        bindable.GestureRecognizers.Add(pan);
        bindable.HandlerChanged += OnHandlerChanged;
#if WINDOWS
        TryHookNative(bindable);
#endif
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.GestureRecognizers.Remove(pan);
        pan.PanUpdated -= OnPanUpdated;
        bindable.HandlerChanged -= OnHandlerChanged;
#if WINDOWS
        UnhookNative();
#endif
        attachedView = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (attachedView is null || DragCommand is null)
        {
            return;
        }

        var payload = new DragPayload(attachedView.BindingContext, e.StatusType, e.TotalX, e.TotalY);
        if (DragCommand.CanExecute(payload))
        {
            DragCommand.Execute(payload);
        }
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        if (attachedView is not null)
        {
            UnhookNative();
            TryHookNative(attachedView);
        }
#endif
    }

#if WINDOWS
    private void TryHookNative(View view)
    {
        nativeElement = view.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
        if (nativeElement is null)
        {
            return;
        }

        nativeElement.ManipulationMode =
            Microsoft.UI.Xaml.Input.ManipulationModes.TranslateX |
            Microsoft.UI.Xaml.Input.ManipulationModes.TranslateY |
            Microsoft.UI.Xaml.Input.ManipulationModes.System;

        nativeElement.ManipulationStarted += OnManipulationStarted;
        nativeElement.ManipulationDelta += OnManipulationDelta;
        nativeElement.ManipulationCompleted += OnManipulationCompleted;
    }

    private void UnhookNative()
    {
        if (nativeElement is null)
        {
            return;
        }

        nativeElement.ManipulationStarted -= OnManipulationStarted;
        nativeElement.ManipulationDelta -= OnManipulationDelta;
        nativeElement.ManipulationCompleted -= OnManipulationCompleted;
        nativeElement = null;
    }

    private void OnManipulationStarted(object sender, Microsoft.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
    {
        if (attachedView is null || DragCommand is null)
        {
            return;
        }

        totalX = 0;
        totalY = 0;
        ExecuteDrag(new DragPayload(attachedView.BindingContext, GestureStatus.Started, 0, 0));
    }

    private void OnManipulationDelta(object sender, Microsoft.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
    {
        if (attachedView is null || DragCommand is null)
        {
            return;
        }

        totalX += e.Delta.Translation.X;
        totalY += e.Delta.Translation.Y;
        ExecuteDrag(new DragPayload(attachedView.BindingContext, GestureStatus.Running, totalX, totalY));
    }

    private void OnManipulationCompleted(object sender, Microsoft.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
    {
        if (attachedView is null || DragCommand is null)
        {
            return;
        }

        ExecuteDrag(new DragPayload(attachedView.BindingContext, GestureStatus.Completed, totalX, totalY));
    }
#endif

    private void ExecuteDrag(DragPayload payload)
    {
        if (DragCommand is null)
        {
            return;
        }

        if (DragCommand.CanExecute(payload))
        {
            DragCommand.Execute(payload);
        }
    }

}

public sealed record DragPayload(object? Item, GestureStatus Status, double TotalX, double TotalY);
