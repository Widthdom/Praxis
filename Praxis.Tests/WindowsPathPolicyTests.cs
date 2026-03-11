using Praxis.Core.Logic;

namespace Praxis.Tests;

public class WindowsPathPolicyTests
{
    [Theory]
    [InlineData(@"\\server\share")]
    [InlineData(@"\\server\share\folder")]
    [InlineData(@"  \\server\share  ")]
    public void IsUncPath_ReturnsTrue_ForUncPaths(string value)
    {
        var result = WindowsPathPolicy.IsUncPath(value);
        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"C:\Temp")]
    [InlineData(@"/mnt/share")]
    [InlineData("server/share")]
    public void IsUncPath_ReturnsFalse_ForNonUncPaths(string? value)
    {
        var result = WindowsPathPolicy.IsUncPath(value);
        Assert.False(result);
    }
}
