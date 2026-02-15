using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Tests;

public class CoreLogicEdgeCaseTests
{
    [Fact]
    public void ThemeModeParser_DefaultsToSystem_WhenValueIsNullOrInvalid()
    {
        Assert.Equal(ThemeMode.System, ThemeModeParser.ParseOrDefault(null));
        Assert.Equal(ThemeMode.System, ThemeModeParser.ParseOrDefault("invalid"));
    }

    [Fact]
    public void ThemeModeParser_ParsesCaseInsensitiveValues()
    {
        Assert.Equal(ThemeMode.Light, ThemeModeParser.ParseOrDefault("light"));
        Assert.Equal(ThemeMode.Dark, ThemeModeParser.ParseOrDefault("DARK"));
        Assert.Equal(ThemeMode.System, ThemeModeParser.ParseOrDefault("System"));
    }

    [Fact]
    public void ThemeModeParser_TrimsWhitespace_ForSystemValue()
    {
        Assert.Equal(ThemeMode.System, ThemeModeParser.ParseOrDefault("  system  "));
    }

    [Fact]
    public void CommandLineBuilder_ReturnsEmpty_WhenToolIsBlank()
    {
        Assert.Equal(string.Empty, CommandLineBuilder.Build("   ", "status"));
        Assert.Equal(string.Empty, CommandLineBuilder.Build(string.Empty, string.Empty));
    }

    [Fact]
    public void CommandLineBuilder_TrimsToolAndArguments()
    {
        var result = CommandLineBuilder.Build("  git  ", "  status --short  ");
        Assert.Equal("git status --short", result);
    }

    [Fact]
    public void GridSnapper_UsesDefaultUnit_WhenUnitIsInvalid()
    {
        var result = GridSnapper.Snap(14, 0);
        Assert.Equal(10, result);
    }

    [Fact]
    public void GridSnapper_ClampWithinArea_ClampsNegativeToZero()
    {
        var result = GridSnapper.ClampWithinArea(-26, -11, 120, 44, 820, 420);
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void ButtonSearchMatcher_ReturnsTrue_ForEmptyQuery()
    {
        var button = new LauncherButtonRecord { ButtonText = "Open" };
        Assert.True(ButtonSearchMatcher.IsMatch(button, ""));
        Assert.True(ButtonSearchMatcher.IsMatch(button, "   "));
    }

    [Fact]
    public void ButtonSearchMatcher_IsCaseInsensitive()
    {
        var button = new LauncherButtonRecord { Note = "Release Notes" };
        Assert.True(ButtonSearchMatcher.IsMatch(button, "release"));
        Assert.True(ButtonSearchMatcher.IsMatch(button, "NOTES"));
    }

    [Fact]
    public void LogRetentionPolicy_UsesOneDayMinimum_WhenRetentionDaysIsZero()
    {
        var now = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);
        var logs = new[]
        {
            new LaunchLogEntry { TimestampUtc = now.AddDays(-2) },
            new LaunchLogEntry { TimestampUtc = now.AddHours(-12) },
        };

        var deleting = LogRetentionPolicy.GetEntriesToDelete(logs, now, 0);
        Assert.Single(deleting);
    }

    [Fact]
    public void LogRetentionPolicy_DoesNotDelete_ExactlyAtThreshold()
    {
        var now = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);
        var threshold = now.AddDays(-30);
        var logs = new[]
        {
            new LaunchLogEntry { TimestampUtc = threshold },
            new LaunchLogEntry { TimestampUtc = threshold.AddSeconds(-1) },
        };

        var deleting = LogRetentionPolicy.GetEntriesToDelete(logs, now, 30);
        Assert.Single(deleting);
        Assert.Equal(threshold.AddSeconds(-1), deleting[0].TimestampUtc);
    }
}
