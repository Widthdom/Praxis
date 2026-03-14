using Praxis.Core.Logic;
#if MACCATALYST
using UIKit;
#endif

namespace Praxis;

public partial class MainPage
{
    private void ModalNoteEditor_HandlerChanged(object? sender, EventArgs e)
    {
        ApplyModalEditorThemeTextColors();
#if MACCATALYST
        if (ModalNoteEditor.Handler?.PlatformView is UITextView textView)
        {
            textView.BackgroundColor = UIColor.Clear;
            textView.Layer.BorderWidth = 0;
            textView.Layer.CornerRadius = 0;
        }
        ApplyMacNoteEditorVisualState();
        ModalNoteFocusUnderline.IsVisible = ModalNoteEditor.IsFocused;
        ApplyMacEditorKeyCommands();
#endif
#if WINDOWS
        EnsureWindowsTextBoxHooks();
#endif
        UpdateModalEditorHeights();
    }

    private void ModalClipWordEditor_HandlerChanged(object? sender, EventArgs e)
    {
        ApplyModalEditorThemeTextColors();
#if MACCATALYST
        if (ModalClipWordEditor.Handler?.PlatformView is UITextView textView)
        {
            textView.BackgroundColor = UIColor.Clear;
            textView.Layer.BorderWidth = 0;
            textView.Layer.CornerRadius = 0;
        }
        ApplyMacClipWordEditorVisualState();
        ModalClipWordFocusUnderline.IsVisible = ModalClipWordEditor.IsFocused;
        ApplyMacEditorKeyCommands();
#endif
#if WINDOWS
        EnsureWindowsTextBoxHooks();
#endif
        UpdateModalEditorHeights();
    }

    private void ModalClipWordEditor_Focused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        ClearMacModalPseudoFocus();
        ModalClipWordFocusUnderline.IsVisible = true;
#endif
    }

    private void ModalClipWordEditor_Unfocused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        ModalClipWordFocusUnderline.IsVisible = false;
#endif
    }

    private void ModalNoteEditor_Focused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        ClearMacModalPseudoFocus();
        ModalNoteFocusUnderline.IsVisible = true;
#endif
    }

    private void ModalNoteEditor_Unfocused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        ModalNoteFocusUnderline.IsVisible = false;
#endif
    }

    private void ModalActionButton_Unfocused(object? sender, FocusEventArgs e)
    {
#if WINDOWS
        QueueWindowsEditorFocusRestore();
#endif
    }

    private void ModalInvertThemeToggle_Tapped(object? sender, TappedEventArgs e)
    {
        viewModel.Editor.UseInvertedThemeColors = !viewModel.Editor.UseInvertedThemeColors;
        ModalInvertThemeCheckBox.Focus();
    }

    private void ModalGuidEntry_HandlerChanged(object? sender, EventArgs e)
    {
#if MACCATALYST
        EnsureMacGuidEntryReadOnlyBehavior();
#endif
#if WINDOWS
        EnsureWindowsTextBoxHooks();
#endif
    }

    private void ModalTextInput_HandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        EnsureWindowsTextBoxHooks();
#endif
    }

    private void ApplyModalEditorThemeTextColors()
    {
        var dark = IsDarkThemeActive();
        var textColor = Color.FromArgb(ThemeTextColorPolicy.ResolveTextColorHex(dark));
        ModalClipWordEditor.TextColor = textColor;
        ModalNoteEditor.TextColor = textColor;
    }

    private void ModalEditorField_Focused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        if (ReferenceEquals(sender, ModalGuidEntry))
        {
            EnsureMacGuidEntryReadOnlyBehavior();
        }

        ClearMacModalPseudoFocus();
#endif
    }

    private void ModalNoteEditor_TextChanged(object? sender, TextChangedEventArgs e)
    {
#if MACCATALYST
        TryHandleMacEditorTabTextInsertion(sender, e);
#endif
        UpdateModalEditorHeights(noteTextOverride: e.NewTextValue);
    }

    private void ModalClipWordEditor_TextChanged(object? sender, TextChangedEventArgs e)
    {
#if MACCATALYST
        TryHandleMacEditorTabTextInsertion(sender, e);
#endif
        UpdateModalEditorHeights(clipTextOverride: e.NewTextValue);
    }

    private void UpdateModalEditorHeights(string? clipTextOverride = null, string? noteTextOverride = null)
    {
        if (!xamlLoaded || EditorFieldsScrollView is null)
        {
            return;
        }

        var clipText = clipTextOverride ?? ModalClipWordEditor.Text ?? viewModel.Editor.ClipText;
        var noteText = noteTextOverride ?? ModalNoteEditor.Text ?? viewModel.Editor.Note;
        var clipHeight = ModalEditorHeightResolver.ResolveHeight(clipText);
        var noteHeight = ModalEditorHeightResolver.ResolveHeight(noteText);

        UpdateEditorHeight(ModalClipWordEditor, ModalClipWordContainer, CopyClipWordButton, clipHeight);
        UpdateEditorHeight(ModalNoteEditor, ModalNoteContainer, CopyNoteButton, noteHeight);

        var contentHeight = ResolveModalEditorScrollContentHeight(clipHeight, noteHeight);
        var maxHeight = ResolveModalEditorScrollMaxHeight();
        EditorFieldsScrollView.HeightRequest = ModalEditorScrollHeightResolver.Resolve(contentHeight, maxHeight);
        EditorFieldsScrollView.InvalidateMeasure();

        ModalClipWordContainer.InvalidateMeasure();
        ModalNoteContainer.InvalidateMeasure();
        EditorOverlay.InvalidateMeasure();
    }

    private static void UpdateEditorHeight(Editor editor, Border container, Button? copyButton, double targetHeight)
    {
        editor.HeightRequest = targetHeight;
        container.HeightRequest = targetHeight;
        if (copyButton is not null)
        {
            copyButton.HeightRequest = targetHeight;
        }
    }

    private static double ResolveModalEditorScrollContentHeight(double clipHeight, double noteHeight)
    {
        var dynamicHeight = clipHeight + noteHeight;
        var staticRowsHeight = ModalStaticRows * ModalSingleLineRowHeight;
        var totalSpacing = (ModalTotalRows - 1) * ModalRowSpacing;
        return staticRowsHeight + dynamicHeight + totalSpacing;
    }

    private double ResolveModalEditorScrollMaxHeight()
    {
        var hostHeight = RootGrid.Height > 0 ? RootGrid.Height : Height;
        if (hostHeight <= 0)
        {
            return ModalScrollMaxHeightFallback;
        }

        return Math.Max(180, hostHeight - ModalScrollVerticalReserve);
    }
}
