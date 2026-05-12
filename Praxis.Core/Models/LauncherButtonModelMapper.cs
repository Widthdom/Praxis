namespace Praxis.Core.Models;

public static class LauncherButtonModelMapper
{
    public static LauncherButtonModel FromRecord(LauncherButtonRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new LauncherButtonModel
        {
            Id = record.Id,
            Command = record.Command,
            Text = string.IsNullOrWhiteSpace(record.ButtonText) ? record.Command : record.ButtonText,
            CommandPath = record.Tool,
            Arguments = record.Arguments,
            ClipText = record.ClipText,
            Note = record.Note,
            X = record.X,
            Y = record.Y,
            Width = record.Width,
            Height = record.Height,
            UseInvertedThemeColors = record.UseInvertedThemeColors,
            ColorKey = record.ColorKey,
            ToolTip = ResolveToolTip(record),
            LastExecutedAtUtc = record.LastExecutedAtUtc,
            SortOrder = record.SortOrder,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc,
        };
    }

    public static LauncherButtonRecord ToRecord(LauncherButtonModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new LauncherButtonRecord
        {
            Id = model.Id,
            Command = model.Command,
            ButtonText = model.Text,
            Tool = model.CommandPath,
            Arguments = model.Arguments,
            ClipText = model.ClipText,
            Note = model.Note,
            X = model.X,
            Y = model.Y,
            Width = model.Width,
            Height = model.Height,
            UseInvertedThemeColors = model.UseInvertedThemeColors,
            ColorKey = model.ColorKey,
            ToolTip = model.ToolTip,
            LastExecutedAtUtc = model.LastExecutedAtUtc,
            SortOrder = model.SortOrder,
            CreatedAtUtc = model.CreatedAtUtc,
            UpdatedAtUtc = model.UpdatedAtUtc,
        };
    }

    private static string ResolveToolTip(LauncherButtonRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.ToolTip))
        {
            return record.ToolTip;
        }

        if (!string.IsNullOrWhiteSpace(record.Note))
        {
            return record.Note;
        }

        return string.IsNullOrWhiteSpace(record.Command) ? record.ButtonText : record.Command;
    }
}
