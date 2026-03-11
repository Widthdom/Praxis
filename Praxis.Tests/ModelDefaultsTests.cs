using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Tests;

public class ModelDefaultsTests
{
    [Fact]
    public void LauncherButtonRecord_Defaults_AreInitializedAsExpected()
    {
        var record = new LauncherButtonRecord();

        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(string.Empty, record.Command);
        Assert.Equal(string.Empty, record.ButtonText);
        Assert.Equal(string.Empty, record.Tool);
        Assert.Equal(string.Empty, record.Arguments);
        Assert.Equal(string.Empty, record.ClipText);
        Assert.Equal(string.Empty, record.Note);
        Assert.Equal(0, record.X);
        Assert.Equal(0, record.Y);
        Assert.Equal(ButtonLayoutDefaults.Width, record.Width);
        Assert.Equal(ButtonLayoutDefaults.Height, record.Height);
        Assert.False(record.UseInvertedThemeColors);
        Assert.Equal(DateTimeKind.Utc, record.CreatedAtUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, record.UpdatedAtUtc.Kind);
    }

    [Fact]
    public void LauncherButtonRecord_DefaultIds_AreUniqueAcrossInstances()
    {
        var a = new LauncherButtonRecord();
        var b = new LauncherButtonRecord();

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void LaunchLogEntry_Defaults_AreInitializedAsExpected()
    {
        var entry = new LaunchLogEntry();

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Null(entry.ButtonId);
        Assert.Equal(string.Empty, entry.Source);
        Assert.Equal(string.Empty, entry.Tool);
        Assert.Equal(string.Empty, entry.Arguments);
        Assert.False(entry.Succeeded);
        Assert.Equal(string.Empty, entry.Message);
        Assert.Equal(DateTimeKind.Utc, entry.TimestampUtc.Kind);
    }

    [Fact]
    public void ErrorLogEntry_Defaults_AreInitializedAsExpected()
    {
        var entry = new ErrorLogEntry();

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(string.Empty, entry.Context);
        Assert.Equal(string.Empty, entry.ExceptionType);
        Assert.Equal(string.Empty, entry.Message);
        Assert.Equal(string.Empty, entry.StackTrace);
        Assert.Equal(DateTimeKind.Utc, entry.TimestampUtc.Kind);
    }

    [Fact]
    public void LauncherButtonRecord_CopyConstructorAndClone_CopyAllFields()
    {
        var source = new LauncherButtonRecord
        {
            Id = Guid.Parse("7B492B76-C7E8-4E22-B9D6-4A8F78AD6FDE"),
            Command = "cmd",
            ButtonText = "Button",
            Tool = "tool",
            Arguments = "--arg value",
            ClipText = "clip",
            Note = "note",
            X = 123.5,
            Y = 456.75,
            Width = 321.25,
            Height = 654.5,
            UseInvertedThemeColors = true,
            CreatedAtUtc = new DateTime(2024, 01, 02, 03, 04, 05, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2024, 06, 07, 08, 09, 10, DateTimeKind.Utc),
        };

        var copied = new LauncherButtonRecord(source);
        var cloned = source.Clone();

        Assert.Equal(source.Id, copied.Id);
        Assert.Equal(source.Command, copied.Command);
        Assert.Equal(source.ButtonText, copied.ButtonText);
        Assert.Equal(source.Tool, copied.Tool);
        Assert.Equal(source.Arguments, copied.Arguments);
        Assert.Equal(source.ClipText, copied.ClipText);
        Assert.Equal(source.Note, copied.Note);
        Assert.Equal(source.X, copied.X);
        Assert.Equal(source.Y, copied.Y);
        Assert.Equal(source.Width, copied.Width);
        Assert.Equal(source.Height, copied.Height);
        Assert.Equal(source.UseInvertedThemeColors, copied.UseInvertedThemeColors);
        Assert.Equal(source.CreatedAtUtc, copied.CreatedAtUtc);
        Assert.Equal(source.UpdatedAtUtc, copied.UpdatedAtUtc);

        Assert.Equal(source.Id, cloned.Id);
        Assert.Equal(source.Command, cloned.Command);
        Assert.Equal(source.ButtonText, cloned.ButtonText);
        Assert.Equal(source.Tool, cloned.Tool);
        Assert.Equal(source.Arguments, cloned.Arguments);
        Assert.Equal(source.ClipText, cloned.ClipText);
        Assert.Equal(source.Note, cloned.Note);
        Assert.Equal(source.X, cloned.X);
        Assert.Equal(source.Y, cloned.Y);
        Assert.Equal(source.Width, cloned.Width);
        Assert.Equal(source.Height, cloned.Height);
        Assert.Equal(source.UseInvertedThemeColors, cloned.UseInvertedThemeColors);
        Assert.Equal(source.CreatedAtUtc, cloned.CreatedAtUtc);
        Assert.Equal(source.UpdatedAtUtc, cloned.UpdatedAtUtc);

        source.Command = "mutated";
        Assert.Equal("cmd", copied.Command);
        Assert.Equal("cmd", cloned.Command);
    }
}
