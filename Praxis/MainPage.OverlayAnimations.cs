using Praxis.Services;

namespace Praxis;

public partial class MainPage
{
    private void RequestEditorOverlayAnimation(bool show)
    {
        editorOverlayFadeCts?.Cancel();
        editorOverlayFadeCts?.Dispose();
        editorOverlayFadeCts = new CancellationTokenSource();
        _ = AnimateOverlayFadeAsync(EditorOverlay, show, EditorOverlayFadeInMs, EditorOverlayFadeOutMs, editorOverlayFadeCts.Token);
    }

    private void RequestContextMenuOverlayAnimation(bool show)
    {
        contextMenuOverlayFadeCts?.Cancel();
        contextMenuOverlayFadeCts?.Dispose();
        contextMenuOverlayFadeCts = new CancellationTokenSource();
        _ = AnimateOverlayFadeAsync(ContextMenuOverlay, show, ContextMenuOverlayFadeInMs, ContextMenuOverlayFadeOutMs, contextMenuOverlayFadeCts.Token);
    }

    private void RequestConflictOverlayAnimation(bool show)
    {
        conflictOverlayFadeCts?.Cancel();
        conflictOverlayFadeCts?.Dispose();
        conflictOverlayFadeCts = new CancellationTokenSource();
        _ = AnimateOverlayFadeAsync(ConflictOverlay, show, ConflictOverlayFadeInMs, ConflictOverlayFadeOutMs, conflictOverlayFadeCts.Token);
    }

    private void RequestCommandSuggestionOverlayAnimation(bool show)
    {
        commandSuggestionOverlayFadeCts?.Cancel();
        commandSuggestionOverlayFadeCts?.Dispose();
        commandSuggestionOverlayFadeCts = new CancellationTokenSource();
        _ = AnimateOverlayFadeAsync(CommandSuggestionPopup, show, CommandSuggestionOverlayFadeInMs, CommandSuggestionOverlayFadeOutMs, commandSuggestionOverlayFadeCts.Token);
    }

    private static async Task AnimateOverlayFadeAsync(VisualElement overlay, bool show, uint fadeInMs, uint fadeOutMs, CancellationToken token)
    {
        try
        {
            if (show)
            {
                overlay.CancelAnimations();
                overlay.InputTransparent = false;
                if (!overlay.IsVisible)
                {
                    overlay.Opacity = 0;
                    overlay.IsVisible = true;
                }

                if (overlay.Opacity < 1.0)
                {
                    await overlay.FadeToAsync(1.0, fadeInMs, Easing.CubicOut);
                }
            }
            else
            {
                if (!overlay.IsVisible)
                {
                    return;
                }

                overlay.CancelAnimations();
                overlay.InputTransparent = true;
                await overlay.FadeToAsync(0.0, fadeOutMs, Easing.CubicIn);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                overlay.IsVisible = false;
                overlay.Opacity = 0;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(AnimateOverlayFadeAsync), $"Overlay fade failed for show={show}: {safeMessage}");
        }
    }
}
