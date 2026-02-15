using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Tests;

public class CoreLogicPerformanceSafetyTests
{
    [Fact]
    public void ButtonLayoutDefaults_AreExpectedValues()
    {
        Assert.Equal(120, ButtonLayoutDefaults.Width);
        Assert.Equal(40, ButtonLayoutDefaults.Height);
    }

    [Fact]
    public void ButtonLayoutDefaults_AreGridFriendlyMultiplesOfTen()
    {
        Assert.Equal(0, ButtonLayoutDefaults.Width % 10);
        Assert.Equal(0, ButtonLayoutDefaults.Height % 10);
    }

    [Fact]
    public void LauncherButtonRecord_DefaultSize_UsesButtonLayoutDefaults()
    {
        var record = new LauncherButtonRecord();
        Assert.Equal(ButtonLayoutDefaults.Width, record.Width);
        Assert.Equal(ButtonLayoutDefaults.Height, record.Height);
    }

    [Fact]
    public void ThemeModeParser_UsesProvidedDefault_WhenValueIsInvalid()
    {
        var parsed = ThemeModeParser.ParseOrDefault("not-a-theme", ThemeMode.Light);
        Assert.Equal(ThemeMode.Light, parsed);
    }

    [Fact]
    public void ThemeModeParser_TrimsWhitespace_BeforeParsing()
    {
        var parsed = ThemeModeParser.ParseOrDefault("  dark  ");
        Assert.Equal(ThemeMode.Dark, parsed);
    }

    [Theory]
    [InlineData(5, 10)]
    [InlineData(15, 20)]
    [InlineData(-5, -10)]
    [InlineData(-15, -20)]
    public void GridSnapper_Snap_UsesAwayFromZeroMidpointRounding(double input, double expected)
    {
        Assert.Equal(expected, GridSnapper.Snap(input));
    }

    [Fact]
    public void GridSnapper_ClampWithinArea_ReturnsOrigin_WhenButtonIsLargerThanArea()
    {
        var result = GridSnapper.ClampWithinArea(180, 260, width: 500, height: 300, areaWidth: 320, areaHeight: 240);
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void CommandLineBuilder_ReturnsToolOnly_WhenArgumentsAreWhitespace()
    {
        var commandLine = CommandLineBuilder.Build("pwsh", "   ");
        Assert.Equal("pwsh", commandLine);
    }

    [Fact]
    public void ButtonSearchMatcher_HandlesNullFieldValues_WithoutThrowing()
    {
        var button = new LauncherButtonRecord
        {
            Command = null!,
            ButtonText = null!,
            Tool = null!,
            Arguments = null!,
            ClipText = null!,
            Note = null!,
        };

        var ex = Record.Exception(() => ButtonSearchMatcher.IsMatch(button, "abc"));
        Assert.Null(ex);
        Assert.False(ButtonSearchMatcher.IsMatch(button, "abc"));
    }

    [Fact]
    public void LogRetentionPolicy_ReturnsEmpty_WhenInputIsEmpty()
    {
        var now = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);
        var deleting = LogRetentionPolicy.GetEntriesToDelete(Array.Empty<LaunchLogEntry>(), now, 30);
        Assert.Empty(deleting);
    }

    [Fact]
    public void LogRetentionPolicy_TreatsNegativeRetention_AsOneDayMinimum()
    {
        var now = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);
        var logs = new[]
        {
            new LaunchLogEntry { TimestampUtc = now.AddDays(-2) },
            new LaunchLogEntry { TimestampUtc = now.AddHours(-20) },
        };

        var deleting = LogRetentionPolicy.GetEntriesToDelete(logs, now, -3);
        Assert.Single(deleting);
        Assert.Equal(now.AddDays(-2), deleting[0].TimestampUtc);
    }

    [Fact]
    public void RecordVersionComparer_DetectsConflict_WhenTimestampsDiffer()
    {
        var original = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc);
        var latest = original.AddSeconds(1);
        Assert.True(RecordVersionComparer.HasConflict(original, latest));
    }

    [Fact]
    public void RecordVersionComparer_NoConflict_WhenTimestampsEqual()
    {
        var ts = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc);
        Assert.False(RecordVersionComparer.HasConflict(ts, ts));
    }
}
