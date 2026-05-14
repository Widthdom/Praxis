namespace Praxis.Tests;

public class CiCoverageWorkflowPolicyTests
{
    [Fact]
    public void CiWorkflow_CollectsAndUploadsCoberturaCoverage()
    {
        var job = ExtractJob("ci.yml", "core-tests");

        Assert.Contains("--collect:\"XPlat Code Coverage\"", job, StringComparison.Ordinal);
        Assert.Contains("--results-directory ./TestResults", job, StringComparison.Ordinal);
        Assert.Contains("name: Upload coverage artifact", job, StringComparison.Ordinal);
        Assert.Contains("path: TestResults/**/coverage.cobertura.xml", job, StringComparison.Ordinal);
        Assert.Contains("name: praxis-test-coverage-cobertura", job, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("core-tests")]
    [InlineData("desktop-build")]
    public void CiWorkflow_EveryJobUsesFullHistoryCheckoutForGitVersioning(string jobName)
    {
        var job = ExtractJob("ci.yml", jobName);

        Assert.Contains("fetch-depth: 0", job, StringComparison.Ordinal);
        Assert.DoesNotContain("fetch-depth: 1", job, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("core-tests")]
    [InlineData("desktop-build")]
    public void CiWorkflow_EveryJobPinsDotnetSdkToGaChannel(string jobName)
    {
        var job = ExtractJob("ci.yml", jobName);

        Assert.Contains("dotnet-version: 10.0.x", job, StringComparison.Ordinal);
        Assert.Contains("dotnet-quality: ga", job, StringComparison.Ordinal);
    }

    [Fact]
    public void CiWorkflow_DesktopBuildUsesAvaloniaProjectWithoutMauiWorkload()
    {
        var job = ExtractJob("ci.yml", "desktop-build");

        Assert.Contains("dotnet build Praxis.Avalonia/Praxis.Avalonia.csproj -c Release -v minimal", job, StringComparison.Ordinal);
        Assert.Contains("windows-latest", job, StringComparison.Ordinal);
        Assert.Contains("macos-latest", job, StringComparison.Ordinal);
        Assert.Contains("ubuntu-latest", job, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet workload install maui", job, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Praxis/Praxis.csproj", job, StringComparison.Ordinal);
    }

    [Fact]
    public void DeliveryWorkflow_PublishesAvaloniaProjectForDesktopRids()
    {
        var job = ExtractJob("delivery.yml", "package");

        Assert.Contains("dotnet publish Praxis.Avalonia/Praxis.Avalonia.csproj", job, StringComparison.Ordinal);
        Assert.Contains("rid: win-x64", job, StringComparison.Ordinal);
        Assert.Contains("rid: osx-x64", job, StringComparison.Ordinal);
        Assert.Contains("rid: linux-x64", job, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet workload install maui", job, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Praxis/Praxis.csproj", job, StringComparison.Ordinal);
    }

    private static string ExtractJob(string fileName, string jobName)
    {
        var workflow = ReadWorkflow(fileName);
        var lines = workflow.Split('\n');
        var header = $"  {jobName}:";
        var startIndex = Array.FindIndex(lines, l => l.TrimEnd('\r') == header);
        if (startIndex < 0)
        {
            throw new InvalidOperationException($"Job '{jobName}' not found in {fileName}.");
        }

        var endIndex = lines.Length;
        for (var i = startIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Length >= 3
                && line[0] == ' '
                && line[1] == ' '
                && line[2] != ' '
                && line.TrimStart().StartsWith(' ') == false)
            {
                endIndex = i;
                break;
            }
        }

        return string.Join('\n', lines, startIndex, endIndex - startIndex);
    }

    private static string ReadWorkflow(string fileName)
    {
        var root = ResolveRepositoryRoot();
        var workflowPath = Path.Combine(root, ".github", "workflows", fileName);
        return File.ReadAllText(workflowPath);
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".github"))
                && Directory.Exists(Path.Combine(current.FullName, "Praxis.Tests")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located from test output path.");
    }
}
