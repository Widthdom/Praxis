using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Tests;

public class CoreLogicTests
{
    [Fact]
    public void CommandLineBuilder_JoinsToolAndArgs()
    {
        var result = CommandLineBuilder.Build("git", "status");
        Assert.Equal("git status", result);
    }

    [Fact]
    public void GridSnapper_ClampsToArea()
    {
        var result = GridSnapper.ClampWithinArea(997, 404, 120, 44, 820, 420);
        Assert.Equal(700, result.X);
        Assert.Equal(376, result.Y);
    }

    [Fact]
    public void ButtonSearchMatcher_MatchesAcrossFields()
    {
        var button = new LauncherButtonRecord
        {
            Command = "open",
            ButtonText = "Open Docs",
            Tool = "cmd",
            Arguments = "/c start",
            ClipText = "https://example.com",
            Note = "documentation",
        };

        Assert.True(ButtonSearchMatcher.IsMatch(button, "docs"));
        Assert.True(ButtonSearchMatcher.IsMatch(button, "example"));
        Assert.False(ButtonSearchMatcher.IsMatch(button, "nomatch"));
    }

    [Fact]
    public void LogRetentionPolicy_ReturnsOnlyExpiredRows()
    {
        var now = new DateTime(2026, 2, 14, 12, 0, 0, DateTimeKind.Utc);
        var logs = new[]
        {
            new LaunchLogEntry { TimestampUtc = now.AddDays(-40) },
            new LaunchLogEntry { TimestampUtc = now.AddDays(-5) },
        };

        var deleting = LogRetentionPolicy.GetEntriesToDelete(logs, now, 30);
        Assert.Single(deleting);
    }
}

