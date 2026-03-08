using Praxis.Core.Logic;

namespace Praxis.Tests;

public class CommandLineBuilderTests
{
    [Fact]
    public void Build_ReturnsEmpty_WhenToolIsNull()
    {
        var commandLine = CommandLineBuilder.Build(null!, "status");

        Assert.Equal(string.Empty, commandLine);
    }

    [Fact]
    public void Build_ReturnsToolOnly_WhenArgumentsIsNull()
    {
        var commandLine = CommandLineBuilder.Build("dotnet", null!);

        Assert.Equal("dotnet", commandLine);
    }

    [Fact]
    public void Build_PreservesInnerSpacing_WhileTrimmingOuterWhitespace()
    {
        var commandLine = CommandLineBuilder.Build("  mytool  ", "  --flag   value  ");

        Assert.Equal("mytool --flag   value", commandLine);
    }
}
