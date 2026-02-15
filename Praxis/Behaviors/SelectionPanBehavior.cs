using System.Windows.Input;

namespace Praxis.Behaviors;

public sealed class SelectionPanBehavior : Behavior<View>
{
    public static readonly BindableProperty SelectionCommandProperty = BindableProperty.Create(
        nameof(SelectionCommand), typeof(ICommand), typeof(SelectionPanBehavior));

    private readonly PanGestureRecognizer pan = new();
    private double startX;
    private double startY;

    public ICommand? SelectionCommand
    {
        get => (ICommand?)GetValue(SelectionCommandProperty);
        set => SetValue(SelectionCommandProperty, value);
    }

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        pan.PanUpdated += OnPanUpdated;
        bindable.GestureRecognizers.Add(pan);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.GestureRecognizers.Remove(pan);
        pan.PanUpdated -= OnPanUpdated;
        base.OnDetachingFrom(bindable);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (SelectionCommand is null)
        {
            return;
        }

        if (e.StatusType == GestureStatus.Started)
        {
            startX = e.TotalX;
            startY = e.TotalY;
        }

        var payload = new SelectionPayload(startX, startY, e.TotalX, e.TotalY, e.StatusType);
        if (SelectionCommand.CanExecute(payload))
        {
            SelectionCommand.Execute(payload);
        }
    }
}

public sealed record SelectionPayload(double StartX, double StartY, double CurrentX, double CurrentY, GestureStatus Status);
