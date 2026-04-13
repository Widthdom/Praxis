using Praxis.Core.Logic;

namespace Praxis.Tests;

public class LaunchTargetResolverTests
{
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/path?q=1")]
    [InlineData("HTTPS://example.com/path?q=1")]
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
    public void Resolve_DetectsRelativePathLikeText()
    {
        var result = LaunchTargetResolver.Resolve("docs/readme.txt");
        Assert.Equal(LaunchTargetKind.FileSystemPath, result.Kind);
        Assert.Equal("docs/readme.txt", result.Target);
    }

    [Theory]
    [InlineData("./docs/readme.txt")]
    [InlineData("../docs/readme.txt")]
    [InlineData(".\\docs\\readme.txt")]
    [InlineData("..\\docs\\readme.txt")]
    public void Resolve_DetectsDotRelativePathLikeText(string value)
    {
        var result = LaunchTargetResolver.Resolve(value);
        Assert.Equal(LaunchTargetKind.FileSystemPath, result.Kind);
        Assert.Equal(value, result.Target);
    }

    [Fact]
    public void Resolve_DetectsBareTilde_AsPathLikeValue()
    {
        var result = LaunchTargetResolver.Resolve("~");
        Assert.Equal(LaunchTargetKind.FileSystemPath, result.Kind);
        Assert.Equal("~", result.Target);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public void Resolve_DetectsCurrentAndParentDirectoryMarkers(string value)
    {
        var result = LaunchTargetResolver.Resolve(value);
        Assert.Equal(LaunchTargetKind.FileSystemPath, result.Kind);
        Assert.Equal(value, result.Target);
    }

    [Fact]
    public void Resolve_ReturnsNone_ForBlankArguments()
    {
        var result = LaunchTargetResolver.Resolve("   ");
        Assert.Equal(LaunchTargetKind.None, result.Kind);
        Assert.Equal(string.Empty, result.Target);
    }

    [Fact]
    public void Resolve_ReturnsNone_ForNullArguments()
    {
        var result = LaunchTargetResolver.Resolve(null);
        Assert.Equal(LaunchTargetKind.None, result.Kind);
        Assert.Equal(string.Empty, result.Target);
    }

    [Fact]
    public void Resolve_ExpandsEnvironmentVariables_ForPathLikeValues()
    {
        const string key = "PRAXIS_TEST_HOME";
        var oldValue = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "/tmp/praxis");
            var result = LaunchTargetResolver.Resolve("%PRAXIS_TEST_HOME%/notes.txt");
            Assert.Equal(LaunchTargetKind.FileSystemPath, result.Kind);
            Assert.Equal("/tmp/praxis/notes.txt", result.Target);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, oldValue);
        }
    }

    [Fact]
    public void Resolve_ExpandsEnvironmentVariables_ThenTrimsWrappingQuotes_ForPathLikeValues()
    {
        const string key = "PRAXIS_TEST_HOME_QUOTED";
        var oldValue = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "\"/tmp/praxis quoted\"");
            var result = LaunchTargetResolver.Resolve("%PRAXIS_TEST_HOME_QUOTED%/notes.txt");
            Assert.Equal(LaunchTargetKind.FileSystemPath, result.Kind);
            Assert.Equal("/tmp/praxis quoted/notes.txt", result.Target);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, oldValue);
        }
    }

    [Fact]
    public void Resolve_ExpandsEnvironmentVariables_ThenTrimsWrappingQuotes_ForHttpUrlValues()
    {
        const string key = "PRAXIS_TEST_URL_QUOTED";
        var oldValue = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "\"https://example.com/path?q=1\"");
            var result = LaunchTargetResolver.Resolve("%PRAXIS_TEST_URL_QUOTED%");
            Assert.Equal(LaunchTargetKind.HttpUrl, result.Kind);
            Assert.Equal("https://example.com/path?q=1", result.Target);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, oldValue);
        }
    }

    [Fact]
    public void Resolve_ExpandsSingleQuotedEnvironmentVariables_ForPathLikeValues()
    {
        const string key = "PRAXIS_TEST_HOME_SINGLE_QUOTED";
        var oldValue = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "'/tmp/praxis single quoted'");
            var result = LaunchTargetResolver.Resolve("%PRAXIS_TEST_HOME_SINGLE_QUOTED%/notes.txt");
            Assert.Equal(LaunchTargetKind.FileSystemPath, result.Kind);
            Assert.Equal("/tmp/praxis single quoted/notes.txt", result.Target);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, oldValue);
        }
    }

    [Fact]
    public void Resolve_ExpandsSingleQuotedEnvironmentVariables_ForHttpUrlValues()
    {
        const string key = "PRAXIS_TEST_URL_SINGLE_QUOTED";
        var oldValue = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "'https://example.com/single?q=1'");
            var result = LaunchTargetResolver.Resolve("%PRAXIS_TEST_URL_SINGLE_QUOTED%");
            Assert.Equal(LaunchTargetKind.HttpUrl, result.Kind);
            Assert.Equal("https://example.com/single?q=1", result.Target);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, oldValue);
        }
    }

    [Fact]
    public void Resolve_TrimsWrappingQuotes_ForPathValues()
    {
        var result = LaunchTargetResolver.Resolve(" \"~/Documents/Praxis\" ");
        Assert.Equal(LaunchTargetKind.FileSystemPath, result.Kind);
        Assert.Equal("~/Documents/Praxis", result.Target);
    }

    [Fact]
    public void Resolve_DetectsPathLikeArguments_ForTildeBackslashStyle()
    {
        var result = LaunchTargetResolver.Resolve("~\\Documents\\Praxis");
        Assert.Equal(LaunchTargetKind.FileSystemPath, result.Kind);
        Assert.Equal("~\\Documents\\Praxis", result.Target);
    }

    [Fact]
    public void Resolve_ReturnsNone_WhenExpandedVariableBecomesWhitespace()
    {
        const string key = "PRAXIS_TEST_BLANK";
        var oldValue = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "   ");
            var result = LaunchTargetResolver.Resolve("%PRAXIS_TEST_BLANK%");
            Assert.Equal(LaunchTargetKind.None, result.Kind);
            Assert.Equal(string.Empty, result.Target);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, oldValue);
        }
    }

    [Fact]
    public void Resolve_ExpandsEnvironmentVariables_ForUncStyleValues()
    {
        const string key = "PRAXIS_TEST_UNC";
        var oldValue = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "\\\\server\\share\\folder");
            var result = LaunchTargetResolver.Resolve("%PRAXIS_TEST_UNC%");
            Assert.Equal(LaunchTargetKind.FileSystemPath, result.Kind);
            Assert.Equal("\\\\server\\share\\folder", result.Target);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, oldValue);
        }
    }

    [Fact]
    public void Resolve_ReturnsNone_ForUnbalancedWrappingQuote()
    {
        var result = LaunchTargetResolver.Resolve("\"https://example.com");
        Assert.Equal(LaunchTargetKind.None, result.Kind);
        Assert.Equal(string.Empty, result.Target);
    }

    [Theory]
    [InlineData("\"C:\\Temp\\file.txt")]
    [InlineData("C:\\Temp\\file.txt\"")]
    [InlineData("\"~/Documents/Praxis")]
    [InlineData("~/Documents/Praxis\"")]
    public void Resolve_ReturnsNone_ForUnbalancedWrappingQuote_OnPathLikeValues(string value)
    {
        var result = LaunchTargetResolver.Resolve(value);
        Assert.Equal(LaunchTargetKind.None, result.Kind);
        Assert.Equal(string.Empty, result.Target);
    }
}
