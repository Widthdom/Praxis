using System.Reflection;
using Praxis.Behaviors;
using Praxis.Core.Logic;
using Praxis.ViewModels;

namespace Praxis;

public partial class MainPage
{
    private void Draggable_PointerPressed(object? sender, PointerEventArgs e)
    {
        HideQuickLookPopup();

        if (sender is not BindableObject bindable)
        {
            return;
        }

#if MACCATALYST
        if (IsOtherMouseFromPlatformArgs(e.PlatformArgs) && bindable.BindingContext is LauncherButtonItemViewModel otherMouseItem)
        {
            if (!App.IsMacApplicationActive() || App.IsActivationSuppressionActive())
            {
                return;
            }

            middlePointerPressReceived = true;
            suppressTapExecuteForItemId = otherMouseItem.Id;
            pointerDragging = false;
            ReleaseCapturedPointer();
            if (viewModel.OpenEditorCommand.CanExecute(otherMouseItem))
            {
                viewModel.OpenEditorCommand.Execute(otherMouseItem);
            }
            return;
        }
#endif

        if (IsMiddlePointerPressed(e) && bindable.BindingContext is LauncherButtonItemViewModel middleItem)
        {
#if MACCATALYST
            if (!App.IsMacApplicationActive() || App.IsActivationSuppressionActive())
            {
                return;
            }
#endif

            middlePointerPressReceived = true;
            suppressTapExecuteForItemId = middleItem.Id;
            pointerDragging = false;
            ReleaseCapturedPointer();
            if (viewModel.OpenEditorCommand.CanExecute(middleItem))
            {
                viewModel.OpenEditorCommand.Execute(middleItem);
            }
            return;
        }

        if (!IsPrimaryPointerPressed(e))
        {
            return;
        }

        if (bindable.BindingContext is LauncherButtonItemViewModel item && IsSelectionModifierPressed(e))
        {
            viewModel.ToggleSelection(item);
            suppressTapExecuteForItemId = item.Id;
            pointerDragging = false;
            ReleaseCapturedPointer();
            return;
        }

 #if MACCATALYST
        if (IsSecondaryPointerPressed(e) && bindable.BindingContext is LauncherButtonItemViewModel secondaryItem)
        {
            suppressTapExecuteForItemId = secondaryItem.Id;
            pointerDragging = false;
            ReleaseCapturedPointer();
            if (viewModel.OpenContextMenuCommand.CanExecute(secondaryItem))
            {
                viewModel.OpenContextMenuCommand.Execute(secondaryItem);
            }
            return;
        }
 #endif

#if !MACCATALYST
        suppressTapExecuteForItemId = null;

        var p = e.GetPosition(this);
        if (p is null)
        {
            return;
        }

        pointerDragging = true;
        pointerStart = p.Value;
        pointerLastDx = 0;
        pointerLastDy = 0;
#if WINDOWS
        TryCapturePointer(sender, e);
#endif
        ExecuteDragFromItem(bindable.BindingContext, GestureStatus.Started, 0, 0);
#endif
    }

    private void Draggable_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!pointerDragging || sender is not BindableObject bindable)
        {
            return;
        }

#if WINDOWS
        // If release happened outside the element, end drag immediately.
        if (!IsPrimaryPointerPressed(e))
        {
            pointerDragging = false;
            ReleaseCapturedPointer();
            ExecuteDragFromItem(bindable.BindingContext, GestureStatus.Completed, pointerLastDx, pointerLastDy);
            return;
        }
#endif

        var p = e.GetPosition(this);
        if (p is null)
        {
            return;
        }

        pointerLastDx = p.Value.X - pointerStart.X;
        pointerLastDy = p.Value.Y - pointerStart.Y;
        ExecuteDragFromItem(bindable.BindingContext, GestureStatus.Running, pointerLastDx, pointerLastDy);
    }

    private void Draggable_PointerReleased(object? sender, PointerEventArgs e)
    {
        if (sender is BindableObject bindableForMiddle &&
            IsMiddlePointerPressed(e) &&
            bindableForMiddle.BindingContext is LauncherButtonItemViewModel middleItem)
        {
            var wasPressed = middlePointerPressReceived;
            middlePointerPressReceived = false;

            if (!wasPressed)
            {
                return;
            }

            suppressTapExecuteForItemId = middleItem.Id;
            pointerDragging = false;
            ReleaseCapturedPointer();
            if (viewModel.OpenEditorCommand.CanExecute(middleItem))
            {
                viewModel.OpenEditorCommand.Execute(middleItem);
            }
            return;
        }

        if (!pointerDragging || sender is not BindableObject bindable)
        {
            return;
        }

        pointerDragging = false;
        ReleaseCapturedPointer();
        var p = e.GetPosition(this);
        var dx = p?.X - pointerStart.X ?? pointerLastDx;
        var dy = p?.Y - pointerStart.Y ?? pointerLastDy;
        ExecuteDragFromItem(bindable.BindingContext, GestureStatus.Completed, dx, dy);
    }

    private void DockButton_PointerPressed(object? sender, PointerEventArgs e)
    {
        HideQuickLookPopup();

        if (sender is not BindableObject bindable || bindable.BindingContext is not LauncherButtonItemViewModel item)
        {
            return;
        }

#if MACCATALYST
        if (IsOtherMouseFromPlatformArgs(e.PlatformArgs))
        {
            if (!App.IsMacApplicationActive() || App.IsActivationSuppressionActive())
            {
                return;
            }

            if (viewModel.OpenEditorCommand.CanExecute(item))
            {
                viewModel.OpenEditorCommand.Execute(item);
            }
            return;
        }
#endif

        if (IsMiddlePointerPressed(e))
        {
#if MACCATALYST
            if (!App.IsMacApplicationActive() || App.IsActivationSuppressionActive())
            {
                return;
            }
#endif

            if (viewModel.OpenEditorCommand.CanExecute(item))
            {
                viewModel.OpenEditorCommand.Execute(item);
            }

            return;
        }

#if MACCATALYST
        if (!IsSecondaryPointerPressed(e))
        {
            return;
        }

        if (viewModel.OpenContextMenuCommand.CanExecute(item))
        {
            viewModel.OpenContextMenuCommand.Execute(item);
        }
#endif
    }

    private void Draggable_SecondaryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not LauncherButtonItemViewModel item)
        {
            return;
        }

        if (viewModel.OpenContextMenuCommand.CanExecute(item))
        {
            viewModel.OpenContextMenuCommand.Execute(item);
        }
    }

    private void DockButton_SecondaryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not LauncherButtonItemViewModel item)
        {
            return;
        }

        if (viewModel.OpenContextMenuCommand.CanExecute(item))
        {
            viewModel.OpenContextMenuCommand.Execute(item);
        }
    }

    private void Selection_PointerPressed(object? sender, PointerEventArgs e)
    {
        CloseCommandSuggestionPopup();

        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen)
        {
            return;
        }

        var point = GetCanvasPointFromPointer(e);
        var viewportPoint = e.GetPosition(PlacementScroll);
        if (point is null || viewportPoint is null)
        {
            return;
        }

        if (IsSecondaryPointerPressed(e))
        {
            if (!IsOnAnyVisibleButton(point.Value))
            {
                _ = OpenCreateEditorFromCanvasPointAsync(point.Value);
            }
            return;
        }

#if MACCATALYST
        if (!IsPrimaryPointerPressed(e) || IsOnAnyVisibleButton(point.Value))
        {
            selectionPanPrimed = false;
            return;
        }

        selectionDragging = true;
        selectionPanPrimed = true;
        selectionStartCanvas = point.Value;
        selectionStartViewport = ClampToPlacementViewport(viewportPoint.Value);
        selectionLastCanvas = selectionStartCanvas;
        selectionLastViewport = selectionStartViewport;
        UpdateSelectionRect(selectionStartViewport, selectionStartViewport);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionStartCanvas.X, selectionStartCanvas.Y, GestureStatus.Started));
#else
        if (!IsPrimaryPointerPressed(e) || IsOnAnyVisibleButton(point.Value))
        {
            return;
        }

        selectionDragging = true;
        selectionStartCanvas = point.Value;
        selectionStartViewport = viewportPoint.Value;
        selectionLastCanvas = selectionStartCanvas;
        selectionLastViewport = selectionStartViewport;
#if WINDOWS
        TryCapturePointer(sender, e);
#endif
        UpdateSelectionRect(selectionStartViewport, selectionStartViewport);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionStartCanvas.X, selectionStartCanvas.Y, GestureStatus.Started));
#endif
    }

    private void Selection_PanUpdated(object? sender, PanUpdatedEventArgs e)
    {
#if WINDOWS
        return;
#else
        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                if (!selectionPanPrimed || !selectionDragging)
                {
                    return;
                }

                selectionLastViewport = selectionStartViewport;
                selectionLastCanvas = selectionStartCanvas;
                UpdateSelectionRect(selectionStartViewport, selectionStartViewport);
                return;
            case GestureStatus.Running:
                if (!selectionDragging)
                {
                    return;
                }

                var viewportPoint = ClampToPlacementViewport(new Point(
                    selectionStartViewport.X + e.TotalX,
                    selectionStartViewport.Y + e.TotalY));
                var canvasPoint = new Point(
                    viewportPoint.X + PlacementScroll.ScrollX,
                    viewportPoint.Y + PlacementScroll.ScrollY);

                selectionLastViewport = viewportPoint;
                selectionLastCanvas = canvasPoint;
                UpdateSelectionRect(selectionStartViewport, viewportPoint);
                ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, canvasPoint.X, canvasPoint.Y, GestureStatus.Running));
                return;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (!selectionDragging)
                {
                    return;
                }

                selectionDragging = false;
                selectionPanPrimed = false;
                UpdateSelectionRect(selectionStartViewport, selectionLastViewport, hide: true);
                ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionLastCanvas.X, selectionLastCanvas.Y, GestureStatus.Completed));
                return;
            default:
                return;
        }
#endif
    }

    private async void PlacementCanvas_SecondaryTapped(object? sender, TappedEventArgs e)
    {
        CloseCommandSuggestionPopup();

        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen)
        {
            return;
        }

        var viewportPoint = e.GetPosition(PlacementScroll);
        if (viewportPoint is null)
        {
            return;
        }

        var canvasPoint = new Point(
            viewportPoint.Value.X + PlacementScroll.ScrollX,
            viewportPoint.Value.Y + PlacementScroll.ScrollY);

        if (IsOnAnyVisibleButton(canvasPoint))
        {
            return;
        }

        await OpenCreateEditorFromCanvasPointAsync(canvasPoint);
    }

    private void Selection_PointerMoved(object? sender, PointerEventArgs e)
    {
#if !MACCATALYST
        if (!selectionDragging)
        {
            return;
        }

#if WINDOWS
        if (!IsPrimaryPointerPressed(e))
        {
            selectionDragging = false;
            ReleaseCapturedPointer();
            UpdateSelectionRect(selectionStartViewport, selectionLastViewport, hide: true);
            ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionLastCanvas.X, selectionLastCanvas.Y, GestureStatus.Completed));
            return;
        }
#endif

        var point = GetCanvasPointFromPointer(e);
        if (point is null)
        {
            return;
        }

        var viewportPoint = e.GetPosition(PlacementScroll);
        if (viewportPoint is null)
        {
            return;
        }

        selectionLastCanvas = point.Value;
        selectionLastViewport = ClampToPlacementViewport(viewportPoint.Value);
        UpdateSelectionRect(selectionStartViewport, selectionLastViewport);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, point.Value.X, point.Value.Y, GestureStatus.Running));
#endif
    }

    private void Selection_PointerReleased(object? sender, PointerEventArgs e)
    {
#if MACCATALYST
        selectionPanPrimed = false;
#else
        if (!selectionDragging)
        {
            return;
        }

        selectionDragging = false;
        ReleaseCapturedPointer();
        var point = GetCanvasPointFromPointer(e) ?? selectionStartCanvas;
        selectionLastCanvas = point;
        selectionLastViewport = ClampToPlacementViewport(e.GetPosition(PlacementScroll) ?? selectionStartViewport);
        UpdateSelectionRect(selectionStartViewport, selectionLastViewport, hide: true);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, point.X, point.Y, GestureStatus.Completed));
#endif
    }

    private void ExecuteDragFromItem(object? item, GestureStatus status, double totalX, double totalY)
    {
        var payload = new DragPayload(item, status, totalX, totalY);
        if (viewModel.DragCommand.CanExecute(payload))
        {
            viewModel.DragCommand.Execute(payload);
        }
    }

    private static bool IsSelectionModifierPressed(PointerEventArgs e)
    {
        var platformArgs = e.PlatformArgs;
        if (platformArgs is null)
        {
            return false;
        }

        if (HasSelectionModifier(platformArgs))
        {
            return true;
        }

        var routed = TryGetProperty(platformArgs, "PointerRoutedEventArgs");
        if (routed is not null && HasSelectionModifier(routed))
        {
            return true;
        }

        var gestureRecognizer = TryGetProperty(platformArgs, "GestureRecognizer");
        if (gestureRecognizer is not null && HasSelectionModifier(gestureRecognizer))
        {
            return true;
        }

        var nativeEvent = TryGetProperty(platformArgs, "Event");
        if (nativeEvent is not null && HasSelectionModifier(nativeEvent))
        {
            return true;
        }

        return false;
    }

    private static bool HasSelectionModifier(object source)
    {
        var keyModifiers = TryGetProperty(source, "KeyModifiers");
        if (keyModifiers is not null)
        {
            var keyModifierText = keyModifiers.ToString() ?? string.Empty;
            if (keyModifierText.Contains("Control", StringComparison.OrdinalIgnoreCase) ||
                keyModifierText.Contains("Command", StringComparison.OrdinalIgnoreCase) ||
                keyModifierText.Contains("Meta", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var modifierFlags = TryGetProperty(source, "ModifierFlags");
        if (modifierFlags is not null)
        {
            var modifierText = modifierFlags.ToString() ?? string.Empty;
            if (modifierText.Contains("Control", StringComparison.OrdinalIgnoreCase) ||
                modifierText.Contains("Command", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var modifiers = TryGetProperty(source, "Modifiers");
        if (modifiers is not null)
        {
            var modifierText = modifiers.ToString() ?? string.Empty;
            if (modifierText.Contains("Control", StringComparison.OrdinalIgnoreCase) ||
                modifierText.Contains("Command", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static object? TryGetProperty(object source, string propertyName)
    {
        var prop = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(source);
    }

    private static bool IsPrimaryPointerPressed(PointerEventArgs e)
    {
#if WINDOWS
        var platformArgs = e.PlatformArgs;
        var routedProp = platformArgs?.GetType().GetProperty("PointerRoutedEventArgs");
        var routed = routedProp?.GetValue(platformArgs) as Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
        return routed?.GetCurrentPoint(null).Properties?.IsLeftButtonPressed == true;
#elif MACCATALYST
        return IsPrimaryFromPlatformArgs(e.PlatformArgs);
#else
        return true;
#endif
    }

    private static bool IsSecondaryPointerPressed(PointerEventArgs e)
    {
#if WINDOWS
        var platformArgs = e.PlatformArgs;
        var routedProp = platformArgs?.GetType().GetProperty("PointerRoutedEventArgs");
        var routed = routedProp?.GetValue(platformArgs) as Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
        return routed?.GetCurrentPoint(null).Properties?.IsRightButtonPressed == true;
#elif MACCATALYST
        return IsSecondaryFromPlatformArgs(e.PlatformArgs);
#else
        return false;
#endif
    }

    private static bool IsMiddlePointerPressed(PointerEventArgs e)
    {
#if WINDOWS
        var platformArgs = e.PlatformArgs;
        var routedProp = platformArgs?.GetType().GetProperty("PointerRoutedEventArgs");
        var routed = routedProp?.GetValue(platformArgs) as Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
        return routed?.GetCurrentPoint(null).Properties?.IsMiddleButtonPressed == true;
#elif MACCATALYST
        return IsMiddleFromPlatformArgs(e.PlatformArgs);
#else
        return false;
#endif
    }

    private static bool IsMiddleFromPlatformArgs(object? platformArgs)
    {
        if (platformArgs is null)
        {
            return false;
        }

        if (IsMiddleFromObject(platformArgs))
        {
            return true;
        }

        var gestureRecognizer = TryGetProperty(platformArgs, "GestureRecognizer");
        if (IsMiddleFromObject(gestureRecognizer))
        {
            return true;
        }

        var nativeEvent = TryGetProperty(platformArgs, "Event");
        if (IsMiddleFromObject(nativeEvent))
        {
            return true;
        }

        return false;
    }

    private static bool IsOtherMouseFromPlatformArgs(object? platformArgs)
    {
        if (platformArgs is null)
        {
            return false;
        }

#if MACCATALYST
        var snapshot = BuildPointerDebugSnapshot(platformArgs);
        if (snapshot.Contains("OtherMouse", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
#endif

        var text = platformArgs.ToString() ?? string.Empty;
        return text.Contains("OtherMouse", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPrimaryFromPlatformArgs(object? platformArgs)
    {
        if (platformArgs is null)
        {
            return true;
        }

        if (IsSecondaryFromPlatformArgs(platformArgs) || IsMiddleFromPlatformArgs(platformArgs))
        {
            return false;
        }

        if (IsPrimaryFromObject(platformArgs))
        {
            return true;
        }

        var gestureRecognizer = TryGetProperty(platformArgs, "GestureRecognizer");
        if (IsPrimaryFromObject(gestureRecognizer))
        {
            return true;
        }

        var nativeEvent = TryGetProperty(platformArgs, "Event");
        if (IsPrimaryFromObject(nativeEvent))
        {
            return true;
        }

        return true;
    }

    private static bool IsSecondaryFromPlatformArgs(object? platformArgs)
    {
        if (platformArgs is null)
        {
            return false;
        }

        if (IsSecondaryFromObject(platformArgs))
        {
            return true;
        }

        var gestureRecognizer = TryGetProperty(platformArgs, "GestureRecognizer");
        if (IsSecondaryFromObject(gestureRecognizer))
        {
            return true;
        }

        var nativeEvent = TryGetProperty(platformArgs, "Event");
        if (IsSecondaryFromObject(nativeEvent))
        {
            return true;
        }

        return false;
    }

    private static bool IsPrimaryFromObject(object? source)
    {
        if (source is null)
        {
            return false;
        }

        var leftPressed = TryGetProperty(source, "IsLeftButtonPressed");
        if (leftPressed is bool pressed && pressed)
        {
            return true;
        }

        var pressedButton = TryGetProperty(source, "PressedButton");
        if (IsPrimaryButtonValue(pressedButton))
        {
            return true;
        }

        var button = TryGetProperty(source, "Button");
        if (IsPrimaryButtonValue(button))
        {
            return true;
        }

        var buttons = TryGetProperty(source, "Buttons");
        if (IsPrimaryButtonValue(buttons))
        {
            return true;
        }

        var buttonMask = TryGetProperty(source, "ButtonMask");
        if (TryConvertToUInt64(buttonMask, out var mask) && mask != 0 && (mask & 0x1) != 0 && (mask & ~0x1UL) == 0)
        {
            return true;
        }

        var buttonNumber = TryGetProperty(source, "ButtonNumber");
        if (TryConvertToInt32(buttonNumber, out var number) && number == 0)
        {
            return true;
        }

        var currentEvent = TryGetProperty(source, "CurrentEvent");
        if (currentEvent is not null && !ReferenceEquals(currentEvent, source))
        {
            return IsPrimaryFromObject(currentEvent);
        }

        return false;
    }

    private static bool IsMiddleFromObject(object? source)
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

        var pressedButton = TryGetProperty(source, "PressedButton");
        if (IsMiddleButtonValue(pressedButton))
        {
            return true;
        }

        var button = TryGetProperty(source, "Button");
        if (IsMiddleButtonValue(button))
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

        var buttonMask = TryGetProperty(source, "ButtonMask");
        if (TryConvertToUInt64(buttonMask, out var mask))
        {
            if ((mask & 0x4) != 0 || (mask & 0x8) != 0 || (mask & 0x10) != 0)
            {
                return true;
            }

            if ((mask & 0x2) != 0 && looksLikeOtherMouse)
            {
                return true;
            }
        }

        var currentEvent = TryGetProperty(source, "CurrentEvent");
        if (currentEvent is not null && !ReferenceEquals(currentEvent, source))
        {
            return IsMiddleFromObject(currentEvent);
        }

        return false;
    }

    private static bool IsSecondaryFromObject(object? source)
    {
        if (source is null)
        {
            return false;
        }

        var rightPressed = TryGetProperty(source, "IsRightButtonPressed");
        if (rightPressed is bool pressed && pressed)
        {
            return true;
        }

        var pressedButton = TryGetProperty(source, "PressedButton");
        if (IsSecondaryButtonValue(pressedButton))
        {
            return true;
        }

        var button = TryGetProperty(source, "Button");
        if (IsSecondaryButtonValue(button))
        {
            return true;
        }

        var buttons = TryGetProperty(source, "Buttons");
        if (IsSecondaryButtonValue(buttons))
        {
            return true;
        }

        var buttonMask = TryGetProperty(source, "ButtonMask");
        if (TryConvertToUInt64(buttonMask, out var mask) && (mask & 0x2) != 0)
        {
            return true;
        }

        var buttonNumber = TryGetProperty(source, "ButtonNumber");
        if (TryConvertToInt32(buttonNumber, out var number) && number == 1)
        {
            return true;
        }

        var currentEvent = TryGetProperty(source, "CurrentEvent");
        if (currentEvent is not null && !ReferenceEquals(currentEvent, source))
        {
            return IsSecondaryFromObject(currentEvent);
        }

        return false;
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

        return int.TryParse(text, out var number) && (number == 2 || number == 3);
    }

    private static bool IsSecondaryButtonValue(object? value)
    {
        if (value is null)
        {
            return false;
        }

        if (IsMiddleButtonValue(value))
        {
            return false;
        }

        var text = value.ToString() ?? string.Empty;
        if (text.Contains("Secondary", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Right", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Button1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(text, out var number) && number == 1;
    }

    private static bool IsPrimaryButtonValue(object? value)
    {
        if (value is null)
        {
            return false;
        }

        var text = value.ToString() ?? string.Empty;
        if (text.Contains("Primary", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Left", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Button0", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(text, out var number) && number == 0;
    }

#if MACCATALYST
    private static string BuildPointerDebugSnapshot(object? platformArgs)
    {
        if (platformArgs is null)
        {
            return string.Empty;
        }

        var segments = new List<string>();
        AppendPointerDebugSnapshot(segments, "args", platformArgs);
        var gestureRecognizer = TryGetProperty(platformArgs, "GestureRecognizer");
        if (gestureRecognizer is not null)
        {
            AppendPointerDebugSnapshot(segments, "gesture", gestureRecognizer);
        }

        var nativeEvent = TryGetProperty(platformArgs, "Event");
        if (nativeEvent is not null)
        {
            AppendPointerDebugSnapshot(segments, "event", nativeEvent);
        }

        return string.Join(" | ", segments);
    }

    private static void AppendPointerDebugSnapshot(List<string> segments, string prefix, object source)
    {
        var type = TryGetProperty(source, "Type");
        var pressedButton = TryGetProperty(source, "PressedButton");
        var button = TryGetProperty(source, "Button");
        var buttons = TryGetProperty(source, "Buttons");
        var buttonMask = TryGetProperty(source, "ButtonMask");
        var buttonNumber = TryGetProperty(source, "ButtonNumber");

        var segment = $"{prefix}.src={source.GetType().Name}";
        if (type is not null) segment += $",type={type}";
        if (pressedButton is not null) segment += $",pressed={pressedButton}";
        if (button is not null) segment += $",button={button}";
        if (buttons is not null) segment += $",buttons={buttons}";
        if (buttonMask is not null) segment += $",mask={buttonMask}";
        if (buttonNumber is not null) segment += $",number={buttonNumber}";
        segments.Add(segment);
    }
#endif

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

    private Point? GetCanvasPointFromPointer(PointerEventArgs e)
    {
        var p = e.GetPosition(PlacementScroll);
        if (p is null)
        {
            return null;
        }

        return new Point(p.Value.X + PlacementScroll.ScrollX, p.Value.Y + PlacementScroll.ScrollY);
    }

    private Point ClampToPlacementViewport(Point point)
    {
        var maxX = Math.Max(0, PlacementScroll.Width);
        var maxY = Math.Max(0, PlacementScroll.Height);
        return new Point(
            Math.Clamp(point.X, 0, maxX),
            Math.Clamp(point.Y, 0, maxY));
    }

    private bool IsOnAnyVisibleButton(Point point)
    {
        foreach (var b in viewModel.VisibleButtons)
        {
            if (point.X >= b.X && point.X <= b.X + b.Width &&
                point.Y >= b.Y && point.Y <= b.Y + b.Height)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateSelectionRect(Point start, Point current, bool hide = false)
    {
        if (hide)
        {
            SelectionRect.IsVisible = false;
            return;
        }

        var x = Math.Min(start.X, current.X);
        var y = Math.Min(start.Y, current.Y);
        var w = Math.Abs(current.X - start.X);
        var h = Math.Abs(current.Y - start.Y);
        SelectionRect.IsVisible = w > 2 && h > 2;
        SelectionRect.TranslationX = x;
        SelectionRect.TranslationY = y;
        SelectionRect.WidthRequest = w;
        SelectionRect.HeightRequest = h;
    }

    private void ExecuteSelectionPayload(SelectionPayload payload)
    {
        viewModel.ApplySelection(payload);
    }

    private async Task OpenCreateEditorFromCanvasPointAsync(Point canvasPoint)
    {
        await viewModel.OpenCreateEditorAtAsync(canvasPoint.X, canvasPoint.Y, useClipboardForArguments: true);
        Dispatcher.DispatchDelayed(UiTimingPolicy.ModalOpenInitialFocusDelay, FocusModalPrimaryEditorField);
    }

    private void FocusModalPrimaryEditorField()
    {
        var shouldSelectAll = modalPrimaryFieldSelectAllPending;
        modalPrimaryFieldSelectAllPending = false;
#if WINDOWS
        EnsureWindowsTextBoxHooks();
        try
        {
            ModalButtonTextEntry.Focus();
        }
        catch
        {
            return;
        }

        if (shouldSelectAll &&
            ResolveWindowsTextBoxForEntry(ModalButtonTextEntry) is Microsoft.UI.Xaml.Controls.TextBox textBox)
        {
            textBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            textBox.SelectAll();
        }
#elif MACCATALYST
        TryFocusModalPrimaryTarget(selectAllText: shouldSelectAll);
#else
        ModalButtonTextEntry.Focus();
#endif
    }

}
