using System.Runtime.InteropServices;

using Praxis.Core.Logic;
using Praxis.ViewModels;
#if MACCATALYST
using UIKit;
#endif

namespace Praxis;

public partial class MainPage
{
    private void Draggable_PanUpdated(object? sender, PanUpdatedEventArgs e)
    {
#if !WINDOWS
        if (sender is not BindableObject bindable)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                panDragItem = bindable.BindingContext;
                panDragLastDx = 0;
                panDragLastDy = 0;
                ExecuteDragFromItem(panDragItem, GestureStatus.Started, 0, 0);
                break;
            case GestureStatus.Running:
                panDragLastDx = e.TotalX;
                panDragLastDy = e.TotalY;
                ExecuteDragFromItem(panDragItem ?? bindable.BindingContext, GestureStatus.Running, panDragLastDx, panDragLastDy);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
            {
                var dx = e.TotalX;
                var dy = e.TotalY;
                if (Math.Abs(dx) < 0.5 && Math.Abs(panDragLastDx) >= 0.5)
                {
                    dx = panDragLastDx;
                }

                if (Math.Abs(dy) < 0.5 && Math.Abs(panDragLastDy) >= 0.5)
                {
                    dy = panDragLastDy;
                }

                ExecuteDragFromItem(panDragItem ?? bindable.BindingContext, GestureStatus.Completed, dx, dy);
                panDragItem = null;
                panDragLastDx = 0;
                panDragLastDy = 0;
                break;
            }
            default:
                break;
        }
#endif
    }

    private async void Draggable_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not LauncherButtonItemViewModel item)
        {
            return;
        }

        if (suppressTapExecuteForItemId == item.Id)
        {
            suppressTapExecuteForItemId = null;
            return;
        }

        if (viewModel.ExecuteButtonCommand.CanExecute(item))
        {
            await viewModel.ExecuteButtonCommand.ExecuteAsync(item);
        }
    }

#if WINDOWS
    private void TryCapturePointer(object? sender, PointerEventArgs e)
    {
        var element = (sender as VisualElement)?.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if (element is null)
        {
            return;
        }

        var platformArgs = e.PlatformArgs;
        var routedProp = platformArgs?.GetType().GetProperty("PointerRoutedEventArgs");
        var routed = routedProp?.GetValue(platformArgs) as Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
        var pointer = routed?.Pointer;
        if (pointer is null)
        {
            return;
        }

        var captureMethod = element.GetType().GetMethod("CapturePointer");
        if (captureMethod?.Invoke(element, [pointer]) is bool captured && captured)
        {
            capturedElement = element;
        }
    }

    private void ReleaseCapturedPointer()
    {
        if (capturedElement is null)
        {
            return;
        }

        var releaseAllMethod = capturedElement.GetType().GetMethod("ReleasePointerCaptures", Type.EmptyTypes);
        releaseAllMethod?.Invoke(capturedElement, null);
        capturedElement = null;
    }
#else
    private void ReleaseCapturedPointer()
    {
    }
#endif

    private void MainCommandEntry_HandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        EnsureWindowsKeyHooks();
        EnsureWindowsTextBoxHooks();
#endif
#if MACCATALYST
        ApplyMacCommandSuggestionKeyCommands();
#endif
        UpdateCommandSuggestionPopupPlacement();
    }

    private void MainCommandEntry_Focused(object? sender, FocusEventArgs e)
    {
        ReopenCommandSuggestionsFromEntry();
    }

    private void MainCommandEntry_PointerPressed(object? sender, PointerEventArgs e)
    {
        ReopenCommandSuggestionsFromEntry();
    }

    private void MainCommandEntry_Tapped(object? sender, TappedEventArgs e)
    {
        ReopenCommandSuggestionsFromEntry();
    }

    private void MainSearchEntry_Focused(object? sender, FocusEventArgs e)
    {
        CloseCommandSuggestionPopup();
    }

    private void MainSearchEntry_PointerPressed(object? sender, PointerEventArgs e)
    {
#if MACCATALYST
        MarkMacSearchFocusUserIntent("MainSearchEntry.PointerPressed");
#endif
    }

    private void MainSearchEntry_Tapped(object? sender, TappedEventArgs e)
    {
#if MACCATALYST
        MarkMacSearchFocusUserIntent("MainSearchEntry.Tapped");
#endif
    }

    private void MainSearchEntry_HandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        EnsureWindowsKeyHooks();
        EnsureWindowsTextBoxHooks();
#endif
    }

    private void ApplyClearButtonGlyphAlignmentTuning()
    {
        var translation = ClearButtonGlyphAlignmentPolicy.ResolveTranslation(OperatingSystem.IsWindows());
        CommandClearGlyph.TranslationX = translation;
        CommandClearGlyph.TranslationY = translation;
        SearchClearGlyph.TranslationX = translation;
        SearchClearGlyph.TranslationY = translation;
    }

    private void CommandClearButton_Tapped(object? sender, TappedEventArgs e)
    {
        if (viewModel.ClearCommandInputCommand.CanExecute(null))
        {
            viewModel.ClearCommandInputCommand.Execute(null);
        }

        FocusEntryAfterClearButtonTap(MainCommandEntry);
    }

    private void SearchClearButton_Tapped(object? sender, TappedEventArgs e)
    {
#if MACCATALYST
        MarkMacSearchFocusUserIntent("SearchClearButton.Tapped");
#endif
        if (viewModel.ClearSearchTextCommand.CanExecute(null))
        {
            viewModel.ClearSearchTextCommand.Execute(null);
        }

        FocusEntryAfterClearButtonTap(MainSearchEntry);
    }

    private void FocusEntryAfterClearButtonTap(Entry entry)
    {
        var retryDelays = ClearButtonRefocusPolicy.ResolveRetryDelays(OperatingSystem.IsWindows());
#if WINDOWS
        EnsureWindowsTextBoxHooks();
        windowsSelectAllOnTabNavigationPending = false;
#endif
        foreach (var delay in retryDelays)
        {
            if (delay <= TimeSpan.Zero)
            {
                Dispatcher.Dispatch(() => ApplyEntryFocusAfterClearButtonTap(entry));
                continue;
            }

            Dispatcher.DispatchDelayed(delay, () => ApplyEntryFocusAfterClearButtonTap(entry));
        }
    }

    private void ApplyEntryFocusAfterClearButtonTap(Entry entry)
    {
        entry.Focus();
#if MACCATALYST
        PlaceMacEntryCaretAtEnd(entry);
#endif
#if WINDOWS
        windowsSelectAllOnTabNavigationPending = false;
        var textBox = ResolveWindowsTextBoxForEntry(entry);
        if (textBox is not null)
        {
            textBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            PlaceWindowsTextBoxCaretAtEnd(textBox);
        }
#endif
    }

#if WINDOWS
    private Microsoft.UI.Xaml.Controls.TextBox? ResolveWindowsTextBoxForEntry(Entry entry)
    {
        if (ReferenceEquals(entry, MainCommandEntry))
        {
            return commandTextBox ?? entry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox;
        }

        if (ReferenceEquals(entry, MainSearchEntry))
        {
            return searchTextBox ?? entry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox;
        }

        return entry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox;
    }

    private static void PlaceWindowsTextBoxCaretAtEnd(Microsoft.UI.Xaml.Controls.TextBox textBox)
    {
        var caretPosition = TextCaretPositionResolver.ResolveTailOffset(textBox.Text);
        textBox.Select(caretPosition, 0);
    }
#endif

    private void ClearButton_PointerEntered(object? sender, PointerEventArgs e)
    {
        SetClearButtonHandCursor(sender, useHandCursor: true);
    }

    private void ClearButton_PointerExited(object? sender, PointerEventArgs e)
    {
        SetClearButtonHandCursor(sender, useHandCursor: false);
    }

    private static void SetClearButtonHandCursor(object? sender, bool useHandCursor)
    {
#if WINDOWS
        if (sender is VisualElement element &&
            element.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement frameworkElement)
        {
            var cursor = useHandCursor
                ? Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand)
                : null;
            NonPublicPropertySetter.TrySet(frameworkElement, "ProtectedCursor", cursor);
        }
#endif

#if MACCATALYST
        if (nsCursorClass == IntPtr.Zero)
        {
            return;
        }

        var cursorSelector = useHandCursor ? pointingHandCursorSelector : arrowCursorSelector;
        var cursor = ObjcMsgSendIntPtr(nsCursorClass, cursorSelector);
        if (cursor != IntPtr.Zero)
        {
            ObjcMsgSendVoid(cursor, setCursorSelector);
        }
#endif
    }

#if MACCATALYST
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr ObjcGetClass(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr SelRegisterName(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSendIntPtr(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void ObjcMsgSendVoid(IntPtr receiver, IntPtr selector);
#endif
}
