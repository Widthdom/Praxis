using Praxis.Core.Logic;

namespace Praxis.Tests;

public class LaunchTargetResolverTests
{
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/path?q=1")]
    public void Resolve_DetectsHttpUrl(string value)
    {
        var result = LaunchTargetResolver.Resolve(value);
        Assert.Equal(LaunchTargetKind.HttpUrl, result.Kind);
        Assert.Equal(value, result.Target);
    }

    [Fact]
    public void Resolve_TrimsWrappingQuotes()
    {
        var result = LaunchTargetResolver.Resolve("\"https://example.com\"");
        Assert.Equal(LaunchTargetKind.HttpUrl, result.Kind);
        Assert.Equal("https://example.com", result.Target);
    }

    [Fact]
    public void Resolve_TrimsSingleQuoteWrapping()
    {
        var result = LaunchTargetResolver.Resolve("'https://example.com'");
        Assert.Equal(LaunchTargetKind.HttpUrl, result.Kind);
        Assert.Equal("https://example.com", result.Target);
    }

    [Theory]
    [InlineData("C:\\Temp\\file.txt")]
    [InlineData("\\\\server\\share\\folder")]
    [InlineData("/Users/test/file.txt")]
    [InlineData("~/Documents")]
    [InlineData("\"C:\\Temp\\file.txt\"")]
    public void Resolve_DetectsPathLikeArguments(string value)
    {
        var result = LaunchTargetResolver.Resolve(value);
        Assert.Equal(LaunchTargetKind.FileSystemPath, result.Kind);
        Assert.False(string.IsNullOrWhiteSpace(result.Target));
    }

    [Fact]
    public void Resolve_DetectsFileUri_AsFileSystemPath()
    {
        var result = LaunchTargetResolver.Resolve("file:///tmp/test.pdf");
        Assert.Equal(LaunchTargetKind.FileSystemPath, result.Kind);
        Assert.False(string.IsNullOrWhiteSpace(result.Target));
    }

    [Fact]
    public void Resolve_ReturnsNone_ForUnsupportedSchemes()
    {
        var result = LaunchTargetResolver.Resolve("ftp://example.com/file.txt");
        Assert.Equal(LaunchTargetKind.None, result.Kind);
        Assert.Equal(string.Empty, result.Target);
    }

    [Fact]
    public void Resolve_ReturnsNone_ForRelativePathLikeText()
    {
        var result = LaunchTargetResolver.Resolve("docs/readme.txt");
        Assert.Equal(LaunchTargetKind.None, result.Kind);
        Assert.Equal(string.Empty, result.Target);
    }

    [Fact]
    public void Resolve_ReturnsNone_ForBlankArguments()
    {
        var result = LaunchTargetResolver.Resolve("   ");
        Assert.Equal(LaunchTargetKind.None, result.Kind);
        Assert.Equal(string.Empty, result.Target);
    }
}
