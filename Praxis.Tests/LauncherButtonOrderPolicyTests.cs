using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Tests;

public class LauncherButtonOrderPolicyTests
{
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
    public void ToSortedList_PreservesExistingOrder_WhenPlacementMatches()
    {
        var first = new LauncherButtonRecord { ButtonText = "First", X = 10, Y = 20 };
        var second = new LauncherButtonRecord { ButtonText = "Second", X = 10, Y = 20 };

        var result = LauncherButtonOrderPolicy.ToSortedList([first, second]);

        Assert.Same(first, result[0]);
        Assert.Same(second, result[1]);
    }
}
