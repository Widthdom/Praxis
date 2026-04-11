using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Tests;

public class LauncherButtonOrderPolicyTests
{
    [Fact]
    public void ToSortedList_ThrowsArgumentNullException_WhenButtonsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() => LauncherButtonOrderPolicy.ToSortedList(null!));
    }

    [Fact]
    public void ToSortedList_ReturnsEmptyList_WhenInputIsEmpty()
    {
        var result = LauncherButtonOrderPolicy.ToSortedList([]);
        Assert.Empty(result);
    }

    [Fact]
    public void ToSortedList_OrdersByYThenX()
    {
        var buttons = new[]
        {
            new LauncherButtonRecord { ButtonText = "B", X = 90, Y = 20 },
            new LauncherButtonRecord { ButtonText = "A", X = 10, Y = 20 },
            new LauncherButtonRecord { ButtonText = "Top", X = 50, Y = 10 },
        };

        var result = LauncherButtonOrderPolicy.ToSortedList(buttons);

        Assert.Equal(["Top", "A", "B"], result.Select(x => x.ButtonText));
    }

    [Fact]
    public void ToSortedList_OrdersNegativeAndDecimalCoordinates_ByYThenX()
    {
        var buttons = new[]
        {
            new LauncherButtonRecord { ButtonText = "LowRight", X = 1.5, Y = 2.5 },
            new LauncherButtonRecord { ButtonText = "TopLeft", X = -10.25, Y = -20.5 },
            new LauncherButtonRecord { ButtonText = "TopRight", X = 3.75, Y = -20.5 },
        };

        var result = LauncherButtonOrderPolicy.ToSortedList(buttons);

        Assert.Equal(["TopLeft", "TopRight", "LowRight"], result.Select(x => x.ButtonText));
    }

    [Fact]
    public void ToSortedList_PreservesExistingOrder_WhenPlacementMatches()
    {
        var first = new LauncherButtonRecord { ButtonText = "First", X = 10, Y = 20 };
        var second = new LauncherButtonRecord { ButtonText = "Second", X = 10, Y = 20 };

        var result = LauncherButtonOrderPolicy.ToSortedList([first, second]);

        Assert.Same(first, result[0]);
        Assert.Same(second, result[1]);
    }

    [Fact]
    public void ToSortedList_IgnoresNullEntries()
    {
        var top = new LauncherButtonRecord { ButtonText = "Top", X = 0, Y = 0 };
        LauncherButtonRecord? missing = null;
        var bottom = new LauncherButtonRecord { ButtonText = "Bottom", X = 0, Y = 10 };

        var result = LauncherButtonOrderPolicy.ToSortedList([bottom, missing!, top]);

        Assert.Equal(["Top", "Bottom"], result.Select(x => x.ButtonText));
    }

    [Fact]
    public void ToSortedList_ReturnsNewList_InstanceIndependentFromSourceCollection()
    {
        var source = new List<LauncherButtonRecord>
        {
            new LauncherButtonRecord { ButtonText = "B", X = 1, Y = 1 },
            new LauncherButtonRecord { ButtonText = "A", X = 0, Y = 0 },
        };

        var result = LauncherButtonOrderPolicy.ToSortedList(source);
        result.RemoveAt(0);

        Assert.Equal(2, source.Count);
        Assert.Single(result);
    }
}
