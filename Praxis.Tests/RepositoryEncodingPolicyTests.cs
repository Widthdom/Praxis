namespace Praxis.Tests;

public class RepositoryEncodingPolicyTests
{
    [Fact]
    public void MacCatalystProgramSource_DoesNotUseUtf8Bom()
    {
        var path = Path.Combine(ResolveRepositoryRoot(), "Praxis", "Platforms", "MacCatalyst", "Program.cs");
        var bytes = File.ReadAllBytes(path);

        Assert.False(HasUtf8Bom(bytes), "Mac Catalyst Program.cs should remain BOM-free so cdidx validate stays clean.");
    }

    private static bool HasUtf8Bom(byte[] bytes)
        => bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Praxis"))
                && Directory.Exists(Path.Combine(current.FullName, "Praxis.Tests")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located from test output path.");
    }
}
