using CommunityToolkit.Mvvm.ComponentModel;

namespace Praxis.Core.Models;

public partial class StatusModel : ObservableObject
{
    [ObservableProperty]
    private LauncherStatusKind kind = LauncherStatusKind.Idle;

    [ObservableProperty]
    private string message = "Ready";

    [ObservableProperty]
    private bool isVisible;

    public void Set(LauncherStatusKind nextKind, string nextMessage)
    {
        Kind = nextKind;
        Message = string.IsNullOrWhiteSpace(nextMessage) ? "Ready" : nextMessage;
        IsVisible = nextKind != LauncherStatusKind.Idle || !string.Equals(Message, "Ready", StringComparison.Ordinal);
    }

    public void Dismiss()
    {
        Kind = LauncherStatusKind.Idle;
        Message = "Ready";
        IsVisible = false;
    }
}
