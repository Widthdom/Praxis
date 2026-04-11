namespace Praxis.Services;

public sealed class MauiClipboardService : IClipboardService
{
    public async Task<string> GetTextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var text = await Clipboard.Default.GetTextAsync().WaitAsync(cancellationToken);
        return text ?? string.Empty;
    }

    public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Clipboard.Default.SetTextAsync(text ?? string.Empty).WaitAsync(cancellationToken);
    }
}
