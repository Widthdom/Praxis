#if MACCATALYST
using UIKit;
#endif

using Microsoft.Maui.Controls;
using Praxis.Controls;

namespace Praxis.Behaviors;

public sealed class MacGlassBackdropBehavior : Behavior<View>
{
#if MACCATALYST
    private const nint BackdropTag = 0x50475842;
    private View? attachedView;
    private UIVisualEffectView? backdropView;
#endif

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
#if MACCATALYST
        attachedView = bindable;
        bindable.PropertyChanged += OnPropertyChanged;
        bindable.HandlerChanged += OnHandlerChanged;
        bindable.SizeChanged += OnSizeChanged;
        ApplyBackdrop();
#endif
    }

    protected override void OnDetachingFrom(View bindable)
    {
#if MACCATALYST
        RemoveBackdrop();
        bindable.SizeChanged -= OnSizeChanged;
        bindable.HandlerChanged -= OnHandlerChanged;
        bindable.PropertyChanged -= OnPropertyChanged;
        attachedView = null;
#endif
        base.OnDetachingFrom(bindable);
    }

#if MACCATALYST
    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        ApplyBackdrop();
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(VisualElement.IsVisible) or nameof(MaterialFrame.CornerRadius) or nameof(MaterialFrame.MacOSBackdropOpacity))
        {
            ApplyBackdrop();
        }
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        UpdateBackdropFrame();
    }

    private void ApplyBackdrop()
    {
        if (attachedView?.Handler?.PlatformView is not UIView platformView)
        {
            return;
        }

        platformView.BackgroundColor = UIColor.Clear;
        platformView.Opaque = false;
        ApplyFrameClipping(platformView);

        backdropView ??= FindBackdrop(platformView) ?? CreateBackdrop();
        if (backdropView.Superview is null)
        {
            platformView.InsertSubview(backdropView, 0);
        }

        backdropView.Alpha = GetBackdropOpacity();
        UpdateBackdropFrame();
    }

    private static UIVisualEffectView CreateBackdrop()
    {
        return new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemThinMaterial))
        {
            Tag = BackdropTag,
            UserInteractionEnabled = false,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
        };
    }

    private static UIVisualEffectView? FindBackdrop(UIView platformView)
    {
        foreach (var subview in platformView.Subviews)
        {
            if (subview is UIVisualEffectView effectView && effectView.Tag == BackdropTag)
            {
                return effectView;
            }
        }

        return null;
    }

    private void UpdateBackdropFrame()
    {
        if (attachedView?.Handler?.PlatformView is not UIView platformView ||
            backdropView is null)
        {
            return;
        }

        backdropView.Frame = platformView.Bounds;
        ApplyFrameClipping(platformView);
    }

    private void ApplyFrameClipping(UIView platformView)
    {
        var cornerRadius = attachedView is MaterialFrame frame
            ? frame.CornerRadius
            : 0;

        platformView.ClipsToBounds = cornerRadius > 0;
        platformView.Layer.CornerRadius = (nfloat)cornerRadius;
        platformView.Layer.MasksToBounds = cornerRadius > 0;

        if (backdropView is not null)
        {
            backdropView.ClipsToBounds = cornerRadius > 0;
            backdropView.Layer.CornerRadius = (nfloat)cornerRadius;
            backdropView.Layer.MasksToBounds = cornerRadius > 0;
        }
    }

    private nfloat GetBackdropOpacity()
    {
        var opacity = attachedView is MaterialFrame frame
            ? frame.MacOSBackdropOpacity
            : 1d;

        return (nfloat)Math.Clamp(opacity, 0d, 1d);
    }

    private void RemoveBackdrop()
    {
        if (backdropView is not null)
        {
            backdropView.RemoveFromSuperview();
            backdropView.Dispose();
            backdropView = null;
        }
    }
#endif
}
