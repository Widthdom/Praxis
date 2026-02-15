using CommunityToolkit.Mvvm.ComponentModel;
using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.ViewModels;

public partial class ButtonEditorViewModel : ObservableObject
{
    public Guid Id { get; private set; }
    [ObservableProperty] private DateTime originalCreatedAtUtc = DateTime.UtcNow;
    [ObservableProperty] private DateTime originalUpdatedAtUtc = DateTime.UtcNow;
    [ObservableProperty] private bool isExistingRecord = true;
    [ObservableProperty] private string guidText = string.Empty;
    [ObservableProperty] private string command = string.Empty;
    [ObservableProperty] private string buttonText = string.Empty;
    [ObservableProperty] private string tool = string.Empty;
    [ObservableProperty] private string arguments = string.Empty;
    [ObservableProperty] private string clipText = string.Empty;
    [ObservableProperty] private string note = string.Empty;
    [ObservableProperty] private double x;
    [ObservableProperty] private double y;
    [ObservableProperty] private double width = ButtonLayoutDefaults.Width;
    [ObservableProperty] private double height = ButtonLayoutDefaults.Height;

    public static ButtonEditorViewModel FromRecord(LauncherButtonRecord record, bool isExistingRecord = true)
        => new()
        {
            Id = record.Id,
            OriginalCreatedAtUtc = record.CreatedAtUtc,
            OriginalUpdatedAtUtc = record.UpdatedAtUtc,
            IsExistingRecord = isExistingRecord,
            GuidText = record.Id.ToString(),
            Command = record.Command,
            ButtonText = record.ButtonText,
            Tool = record.Tool,
            Arguments = record.Arguments,
            ClipText = record.ClipText,
            Note = record.Note,
            X = record.X,
            Y = record.Y,
            Width = record.Width,
            Height = record.Height,
        };

    public LauncherButtonRecord ToRecord() => new()
    {
        Id = Id == Guid.Empty ? Guid.NewGuid() : Id,
        Command = Command.Trim(),
        ButtonText = string.IsNullOrWhiteSpace(ButtonText) ? Command.Trim() : ButtonText.Trim(),
        Tool = Tool.Trim(),
        Arguments = Arguments.Trim(),
        ClipText = ClipText,
        Note = Note,
        X = X,
        Y = Y,
        Width = Width <= 0 ? ButtonLayoutDefaults.Width : Width,
        Height = Height <= 0 ? ButtonLayoutDefaults.Height : Height,
        CreatedAtUtc = OriginalCreatedAtUtc == default ? DateTime.UtcNow : OriginalCreatedAtUtc,
        UpdatedAtUtc = DateTime.UtcNow,
    };
}
