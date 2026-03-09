using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Tests;

public class ButtonHistoryConsistencyPolicyTests
{
    [Fact]
    public void MatchesExpectedVersion_ReturnsTrue_WhenBothMissing()
    {
        Assert.True(ButtonHistoryConsistencyPolicy.MatchesExpectedVersion(null, null));
    }

    [Fact]
    public void MatchesExpectedVersion_ReturnsFalse_WhenExpectedMissingButCurrentExists()
    {
        var current = new LauncherButtonRecord { UpdatedAtUtc = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc) };

        Assert.False(ButtonHistoryConsistencyPolicy.MatchesExpectedVersion(null, current));
    }

    [Fact]
    public void MatchesExpectedVersion_ReturnsFalse_WhenExpectedExistsButCurrentMissing()
    {
        var expected = new LauncherButtonRecord { UpdatedAtUtc = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc) };

        Assert.False(ButtonHistoryConsistencyPolicy.MatchesExpectedVersion(expected, null));
    }

    [Fact]
    public void MatchesExpectedVersion_ReturnsTrue_WhenUpdatedAtMatches()
    {
        var timestamp = new DateTime(2025, 04, 05, 6, 7, 8, DateTimeKind.Utc);
        var expected = new LauncherButtonRecord { UpdatedAtUtc = timestamp };
        var current = new LauncherButtonRecord { UpdatedAtUtc = timestamp };

        Assert.True(ButtonHistoryConsistencyPolicy.MatchesExpectedVersion(expected, current));
    }

    [Fact]
    public void MatchesExpectedVersion_ReturnsTrue_WhenOnlyUpdatedAtDiffers()
    {
        var expected = new LauncherButtonRecord
        {
            Id = Guid.Parse("48DDE5F4-50FA-4B22-A6C2-D38A83361A6B"),
            Command = "open",
            ButtonText = "Docs",
            Tool = "cmd",
            Arguments = "/c start",
            ClipText = "clip",
            Note = "note",
            X = 12,
            Y = 24,
            Width = 120,
            Height = 44,
            CreatedAtUtc = new DateTime(2025, 04, 05, 6, 7, 8, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2025, 04, 05, 6, 7, 8, DateTimeKind.Utc),
        };
        var current = expected.Clone();
        current.UpdatedAtUtc = expected.UpdatedAtUtc.AddSeconds(1);

        Assert.True(ButtonHistoryConsistencyPolicy.MatchesExpectedVersion(expected, current));
    }

    [Fact]
    public void MatchesExpectedVersion_ReturnsFalse_WhenContentDiffersEvenIfIdMatches()
    {
        var expected = new LauncherButtonRecord
        {
            Id = Guid.Parse("F57E6074-1721-4FA8-A591-31ACAE541E70"),
            Command = "open",
            ButtonText = "Docs",
            Tool = "cmd",
            Arguments = "/c start",
            ClipText = "clip",
            Note = "note",
            X = 12,
            Y = 24,
            Width = 120,
            Height = 44,
            CreatedAtUtc = new DateTime(2025, 04, 05, 6, 7, 8, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2025, 04, 05, 6, 7, 8, DateTimeKind.Utc),
        };
        var current = expected.Clone();
        current.Note = "changed";
        current.UpdatedAtUtc = expected.UpdatedAtUtc.AddSeconds(1);

        Assert.False(ButtonHistoryConsistencyPolicy.MatchesExpectedVersion(expected, current));
    }

    [Fact]
    public void MatchesExpectedVersion_ReturnsFalse_WhenInvertedThemeFlagDiffers()
    {
        var expected = new LauncherButtonRecord
        {
            Id = Guid.Parse("E8C80058-6709-4CF2-8E57-D9E18A6B9965"),
            Command = "open",
            ButtonText = "Docs",
            Tool = "cmd",
            Arguments = "/c start",
            ClipText = "clip",
            Note = "note",
            X = 12,
            Y = 24,
            Width = 120,
            Height = 44,
            UseInvertedThemeColors = false,
            CreatedAtUtc = new DateTime(2025, 04, 05, 6, 7, 8, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2025, 04, 05, 6, 7, 8, DateTimeKind.Utc),
        };
        var current = expected.Clone();
        current.UseInvertedThemeColors = true;
        current.UpdatedAtUtc = expected.UpdatedAtUtc.AddSeconds(1);

        Assert.False(ButtonHistoryConsistencyPolicy.MatchesExpectedVersion(expected, current));
    }
}
