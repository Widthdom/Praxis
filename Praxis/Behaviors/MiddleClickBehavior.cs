using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Reflection;
using Microsoft.Maui.ApplicationModel;
using Praxis.ViewModels;
#if MACCATALYST
using Foundation;
using UIKit;
#endif

namespace Praxis.Behaviors;

public sealed class MiddleClickBehavior : Behavior<View>
{
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(MiddleClickBehavior));

    private readonly PointerGestureRecognizer pointer = new();
    private View? attachedView;
    private bool executedForCurrentPress;
#if MACCATALYST
    private UIView? attachedNativeView;
    private UILongPressGestureRecognizer? middlePressMask2Recognizer;
    private UILongPressGestureRecognizer? middlePressMask4Recognizer;
    private UILongPressGestureRecognizer? middlePressMask8Recognizer;
    private UILongPressGestureRecognizer? middlePressMask16Recognizer;
    private CancellationTokenSource? deferredSecondaryMaskCts;
#endif

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        attachedView = bindable;
        pointer.PointerPressed += OnPointerPressed;
        pointer.PointerReleased += OnPointerReleased;
        bindable.GestureRecognizers.Add(pointer);
#if MACCATALYST
        bindable.HandlerChanged += BindableOnHandlerChanged;
        AttachNativeMiddleTapRecognizers(bindable);
#endif
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.GestureRecognizers.Remove(pointer);
        pointer.PointerReleased -= OnPointerReleased;
        pointer.PointerPressed -= OnPointerPressed;
#if MACCATALYST
        bindable.HandlerChanged -= BindableOnHandlerChanged;
        DetachNativeMiddleTapRecognizers();
#endif
        attachedView = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnPointerPressed(object? sender, PointerEventArgs e)
    {
        executedForCurrentPress = ExecuteIfMiddleClick(e);
    }

    private void OnPointerReleased(object? sender, PointerEventArgs e)
    {
        if (!executedForCurrentPress)
        {
            _ = ExecuteIfMiddleClick(e);
        }

        executedForCurrentPress = false;
    }

    private bool ExecuteIfMiddleClick(PointerEventArgs e)
    {
        if (attachedView is null || Command is null)
        {
            return false;
        }

        if (!IsMiddlePointerPressed(e))
        {
            return false;
        }

        var param = attachedView.BindingContext;
        if (Command.CanExecute(param))
        {
            Command.Execute(param);
            return true;
        }

        return false;
    }

#if MACCATALYST
    private void BindableOnHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is View view)
        {
            AttachNativeMiddleTapRecognizers(view);
        }
    }

    private void AttachNativeMiddleTapRecognizers(View view)
    {
        var nativeView = view.Handler?.PlatformView as UIView;
        if (ReferenceEquals(nativeView, attachedNativeView))
        {
            return;
        }

        DetachNativeMiddleTapRecognizers();
        attachedNativeView = nativeView;
        if (attachedNativeView is null)
        {
            return;
        }

        middlePressMask2Recognizer = CreateMiddlePressRecognizer(0x2);
        middlePressMask4Recognizer = CreateMiddlePressRecognizer(0x4);
        middlePressMask8Recognizer = CreateMiddlePressRecognizer(0x8);
        middlePressMask16Recognizer = CreateMiddlePressRecognizer(0x10);
        attachedNativeView.AddGestureRecognizer(middlePressMask2Recognizer);
        attachedNativeView.AddGestureRecognizer(middlePressMask4Recognizer);
        attachedNativeView.AddGestureRecognizer(middlePressMask8Recognizer);
        attachedNativeView.AddGestureRecognizer(middlePressMask16Recognizer);
    }

    private UILongPressGestureRecognizer CreateMiddlePressRecognizer(nuint mask)
    {
        var recognizer = new UILongPressGestureRecognizer(() =>
        {
            if (attachedNativeView is null)
            {
                return;
            }

            if (GetRecognizerState(mask) != UIGestureRecognizerState.Began)
            {
                return;
            }

            if (mask == 0x2)
            {
                ExecuteFromSecondaryMaskWithGuard();
                return;
            }

            ExecuteBoundCommand();
        })
        {
            CancelsTouchesInView = false,
            DelaysTouchesBegan = false,
            DelaysTouchesEnded = false,
            NumberOfTouchesRequired = 1,
            MinimumPressDuration = 0.01,
            AllowableMovement = nfloat.MaxValue,
        };
        TrySetButtonMaskRequired(recognizer, mask);
        return recognizer;
    }

    private UIGestureRecognizerState GetRecognizerState(nuint mask)
    {
        return mask switch
        {
            0x2 => middlePressMask2Recognizer?.State ?? UIGestureRecognizerState.Possible,
            0x4 => middlePressMask4Recognizer?.State ?? UIGestureRecognizerState.Possible,
            0x8 => middlePressMask8Recognizer?.State ?? UIGestureRecognizerState.Possible,
            0x10 => middlePressMask16Recognizer?.State ?? UIGestureRecognizerState.Possible,
            _ => UIGestureRecognizerState.Possible,
        };
    }

    private static void TrySetButtonMaskRequired(UILongPressGestureRecognizer recognizer, nuint mask)
    {
        try
        {
            recognizer.SetValueForKey(NSNumber.FromUInt64(mask), new NSString("buttonMaskRequired"));
        }
        catch
        {
        }
    }

    private void ExecuteFromSecondaryMaskWithGuard()
    {
        deferredSecondaryMaskCts?.Cancel();
        deferredSecondaryMaskCts?.Dispose();
        deferredSecondaryMaskCts = new CancellationTokenSource();
        var token = deferredSecondaryMaskCts.Token;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await Task.Delay(90, token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (IsContextMenuCurrentlyOpen())
                {
                    return;
                }
                ExecuteBoundCommand();
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    private bool IsContextMenuCurrentlyOpen()
    {
        var rootPage = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (rootPage?.BindingContext is MainViewModel vm)
        {
            return vm.IsContextMenuOpen;
        }

        return false;
    }

    private void ExecuteBoundCommand()
    {
        if (attachedView is null || Command is null)
        {
            return;
        }

        var param = attachedView.BindingContext;
        if (Command.CanExecute(param))
        {
            Command.Execute(param);
        }
    }

    private void DetachNativeMiddleTapRecognizers()
    {
        if (attachedNativeView is null)
        {
            return;
        }

        deferredSecondaryMaskCts?.Cancel();
        deferredSecondaryMaskCts?.Dispose();
        deferredSecondaryMaskCts = null;

        if (middlePressMask2Recognizer is not null)
        {
            attachedNativeView.RemoveGestureRecognizer(middlePressMask2Recognizer);
            middlePressMask2Recognizer.Dispose();
            middlePressMask2Recognizer = null;
        }

        if (middlePressMask4Recognizer is not null)
        {
            attachedNativeView.RemoveGestureRecognizer(middlePressMask4Recognizer);
            middlePressMask4Recognizer.Dispose();
            middlePressMask4Recognizer = null;
        }

        if (middlePressMask8Recognizer is not null)
        {
            attachedNativeView.RemoveGestureRecognizer(middlePressMask8Recognizer);
            middlePressMask8Recognizer.Dispose();
            middlePressMask8Recognizer = null;
        }

        if (middlePressMask16Recognizer is not null)
        {
            attachedNativeView.RemoveGestureRecognizer(middlePressMask16Recognizer);
            middlePressMask16Recognizer.Dispose();
            middlePressMask16Recognizer = null;
        }

        attachedNativeView = null;
    }
#endif

    private static bool IsMiddlePointerPressed(PointerEventArgs e)
    {
#if WINDOWS
        var platformArgs = e.PlatformArgs;
        var routedProp = platformArgs?.GetType().GetProperty("PointerRoutedEventArgs");
        var routed = routedProp?.GetValue(platformArgs) as Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
        return routed?.GetCurrentPoint(null).Properties?.IsMiddleButtonPressed == true;
#elif MACCATALYST
        var platformArgs = e.PlatformArgs;
        if (platformArgs is null)
        {
            return false;
        }

        if (IsMiddleInObject(platformArgs))
        {
            return true;
        }

        var gestureRecognizer = TryGetProperty(platformArgs, "GestureRecognizer");
        if (IsMiddleInObject(gestureRecognizer))
        {
            return true;
        }

        var nativeEvent = TryGetProperty(platformArgs, "Event");
        return IsMiddleInObject(nativeEvent);
#else
        return false;
#endif
    }

    private static bool IsMiddleInObject(object? source)
    {
        if (source is null)
        {
            return false;
        }

        var eventTypeText = TryGetProperty(source, "Type")?.ToString() ?? string.Empty;
        if (eventTypeText.Contains("OtherMouse", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var middlePressed = TryGetProperty(source, "IsMiddleButtonPressed");
        if (middlePressed is bool pressed && pressed)
        {
            return true;
        }

        var button = TryGetProperty(source, "Button");
        if (IsMiddleButtonValue(button))
        {
            return true;
        }

        var pressedButton = TryGetProperty(source, "PressedButton");
        if (IsMiddleButtonValue(pressedButton))
        {
            return true;
        }

        var buttons = TryGetProperty(source, "Buttons");
        if (IsMiddleButtonValue(buttons))
        {
            return true;
        }

        var buttonNumber = TryGetProperty(source, "ButtonNumber");
        if (TryConvertToInt32(buttonNumber, out var number) && number >= 2)
        {
            return true;
        }

        var looksLikeOtherMouse = eventTypeText.Contains("OtherMouse", StringComparison.OrdinalIgnoreCase);

        var mask = TryGetProperty(source, "ButtonMask");
        if (TryConvertToUInt64(mask, out var maskValue))
        {
            if ((maskValue & 0x4) != 0 || (maskValue & 0x8) != 0 || (maskValue & 0x10) != 0)
            {
                return true;
            }

            if ((maskValue & 0x2) != 0 && looksLikeOtherMouse)
            {
                return true;
            }
        }

        var currentEvent = TryGetProperty(source, "CurrentEvent");
        if (currentEvent is not null && !ReferenceEquals(currentEvent, source))
        {
            return IsMiddleInObject(currentEvent);
        }

        return false;
    }

    private static bool TryConvertToUInt64(object? value, out ulong number)
    {
        switch (value)
        {
            case null:
                number = 0;
                return false;
            case ulong unsignedLong:
                number = unsignedLong;
                return true;
            case Enum enumValue:
                number = Convert.ToUInt64(enumValue);
                return true;
            default:
                return ulong.TryParse(value.ToString(), out number);
        }
    }

    private static bool TryConvertToInt32(object? value, out int number)
    {
        switch (value)
        {
            case null:
                number = 0;
                return false;
            case int signed:
                number = signed;
                return true;
            case Enum enumValue:
                number = Convert.ToInt32(enumValue);
                return true;
            default:
                return int.TryParse(value.ToString(), out number);
        }
    }

    private static bool IsMiddleButtonValue(object? value)
    {
        if (value is null)
        {
            return false;
        }

        var text = value.ToString() ?? string.Empty;
        if (text.Contains("Middle", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Auxiliary", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Center", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Tertiary", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Other", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Button2", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Button3", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (int.TryParse(text, out var buttonNumber))
        {
            return buttonNumber == 2 || buttonNumber == 3;
        }

        return false;
    }

    private static object? TryGetProperty(object source, string propertyName)
    {
        var prop = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(source);
    }
}
