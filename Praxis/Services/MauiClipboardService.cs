namespace Praxis.Services;

public sealed class MauiClipboardService : IClipboardService
{
    public async Task<string> GetTextAsync(CancellationToken cancellationToken = default)
    {
        var text = await Clipboard.Default.GetTextAsync();
        return text ?? string.Empty;
    }

    public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
        => Clipboard.Default.SetTextAsync(text ?? string.Empty);
}
