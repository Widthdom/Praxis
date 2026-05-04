using Microsoft.Maui.Controls.Shapes;
#if MACCATALYST
using Praxis.Behaviors;
#endif

namespace Praxis.Controls;

public sealed class MaterialFrame : Border
{
    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(
            nameof(CornerRadius),
            typeof(double),
            typeof(MaterialFrame),
            0d,
            propertyChanged: OnCornerRadiusChanged);

    public static readonly BindableProperty MacOSBehindWindowBlurProperty =
        BindableProperty.Create(
            nameof(MacOSBehindWindowBlur),
            typeof(bool),
            typeof(MaterialFrame),
            false,
            propertyChanged: OnMacOSBehindWindowBlurChanged);

    public MaterialFrame()
    {
        StrokeThickness = 1;
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(0) };
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public bool MacOSBehindWindowBlur
    {
        get => (bool)GetValue(MacOSBehindWindowBlurProperty);
        set => SetValue(MacOSBehindWindowBlurProperty, value);
    }

    static void OnCornerRadiusChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var frame = (MaterialFrame)bindable;
        frame.StrokeShape = new RoundRectangle
        {
            CornerRadius = new CornerRadius(Convert.ToDouble(newValue))
        };
    }

    static void OnMacOSBehindWindowBlurChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
#if MACCATALYST
        var frame = (MaterialFrame)bindable;
        frame.UpdateMacBackdropBehavior();
#endif
    }

#if MACCATALYST
    private MacGlassBackdropBehavior? macBackdropBehavior;

    private void UpdateMacBackdropBehavior()
    {
        if (MacOSBehindWindowBlur)
        {
            macBackdropBehavior ??= new MacGlassBackdropBehavior();
            if (!Behaviors.Contains(macBackdropBehavior))
            {
                Behaviors.Add(macBackdropBehavior);
            }
        }
        else if (macBackdropBehavior is not null && Behaviors.Contains(macBackdropBehavior))
        {
            Behaviors.Remove(macBackdropBehavior);
        }
    }
#endif
}
