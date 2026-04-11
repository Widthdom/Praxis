using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Tests;

public class CommandRecordMatcherTests
{
    [Fact]
    public void FindMatches_Throws_WhenRecordsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CommandRecordMatcher.FindMatches(null!, "build"));
    }

    [Fact]
    public void FindMatches_ReturnsAllMatchingCommands_IgnoringCaseAndWhitespace()
    {
        var a = new LauncherButtonRecord { Command = "build", ButtonText = "A" };
        var b = new LauncherButtonRecord { Command = " BUILD ", ButtonText = "B" };
        var c = new LauncherButtonRecord { Command = "run", ButtonText = "C" };

        var result = CommandRecordMatcher.FindMatches([a, b, c], "  BuIlD  ");

        Assert.Equal(2, result.Count);
        Assert.Same(a, result[0]);
        Assert.Same(b, result[1]);
    }

    [Fact]
    public void FindMatches_ReturnsEmpty_WhenInputIsBlank()
    {
        var a = new LauncherButtonRecord { Command = "build", ButtonText = "A" };

        var result = CommandRecordMatcher.FindMatches([a], "   ");

        Assert.Empty(result);
    }

    [Fact]
    public void FindMatches_ReturnsEmpty_WhenNoCommandMatches()
    {
        var a = new LauncherButtonRecord { Command = "build", ButtonText = "A" };

        var result = CommandRecordMatcher.FindMatches([a], "test");

        Assert.Empty(result);
    }

    [Fact]
    public void FindMatches_ReturnsEmpty_WhenCommandInputIsNull()
    {
        var a = new LauncherButtonRecord { Command = "build", ButtonText = "A" };

        var result = CommandRecordMatcher.FindMatches([a], null!);

        Assert.Empty(result);
    }

    [Fact]
    public void FindMatches_IgnoresRecords_WithNullCommandValues()
    {
        var a = new LauncherButtonRecord { Command = null!, ButtonText = "A" };
        var b = new LauncherButtonRecord { Command = "build", ButtonText = "B" };

        var result = CommandRecordMatcher.FindMatches([a, b], "build");

        Assert.Single(result);
        Assert.Same(b, result[0]);
    }

    [Fact]
    public void FindMatches_IgnoresNullRecordEntries()
    {
        var match = new LauncherButtonRecord { Command = "build", ButtonText = "B" };
        LauncherButtonRecord? missing = null;

        var result = CommandRecordMatcher.FindMatches([missing!, match], "build");

        Assert.Single(result);
        Assert.Same(match, result[0]);
    }
}
