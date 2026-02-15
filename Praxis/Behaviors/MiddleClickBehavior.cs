using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Praxis.Behaviors;

public sealed class MiddleClickBehavior : Behavior<View>
{
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(MiddleClickBehavior));

    private readonly PointerGestureRecognizer pointer = new();

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        pointer.PointerPressed += OnPointerPressed;
        bindable.GestureRecognizers.Add(pointer);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.GestureRecognizers.Remove(pointer);
        pointer.PointerPressed -= OnPointerPressed;
        base.OnDetachingFrom(bindable);
    }

    private void OnPointerPressed(object? sender, PointerEventArgs e)
    {
#if WINDOWS
        var platformArgs = e.PlatformArgs;
        var routedProp = platformArgs?.GetType().GetProperty("PointerRoutedEventArgs");
        var routed = routedProp?.GetValue(platformArgs) as Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
        var isMiddle = routed?.GetCurrentPoint(null).Properties?.IsMiddleButtonPressed == true;
        if (!isMiddle)
        {
            return;
        }

        if (sender is not View view || Command is null)
        {
            return;
        }

        var param = view.BindingContext;
        if (Command.CanExecute(param))
        {
            Command.Execute(param);
        }
#endif
    }
}
