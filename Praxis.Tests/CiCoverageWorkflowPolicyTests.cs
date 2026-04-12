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
    [InlineData("windows-build")]
    [InlineData("mac-build")]
    public void CiWorkflow_EveryJobUsesFullHistoryCheckoutForGitVersioning(string jobName)
    {
        var job = ExtractJob("ci.yml", jobName);

        // Nerdbank.GitVersioning needs history for version-height computation.
        Assert.Contains("fetch-depth: 0", job, StringComparison.Ordinal);
        Assert.DoesNotContain("fetch-depth: 1", job, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("core-tests")]
    [InlineData("windows-build")]
    [InlineData("mac-build")]
    public void CiWorkflow_EveryJobPinsDotnetSdkToGaChannel(string jobName)
    {
        var job = ExtractJob("ci.yml", jobName);

        Assert.Contains("dotnet-version: 10.0.x", job, StringComparison.Ordinal);
        Assert.Contains("dotnet-quality: ga", job, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("windows-build")]
    [InlineData("mac-build")]
    public void CiWorkflow_PlatformJobsInstallMauiWorkloadWithoutSkipManifestUpdate(string jobName)
    {
        var job = ExtractJob("ci.yml", jobName);

        Assert.Contains("dotnet workload install maui", job, StringComparison.Ordinal);
        // --skip-manifest-update was explicitly removed; do not reintroduce it casually.
        Assert.DoesNotContain("--skip-manifest-update", job, StringComparison.Ordinal);
    }

    [Fact]
    public void CiWorkflow_MacJobRunsXcodeFirstLaunchAndGuardsCompatibility()
    {
        var job = ExtractJob("ci.yml", "mac-build");

        // Mac Catalyst runs need -runFirstLaunch before build.
        Assert.Contains("sudo xcodebuild -runFirstLaunch", job, StringComparison.Ordinal);
        // The Xcode floor gate exists to skip Mac Catalyst when Xcode is too old.
        Assert.Contains("Check Xcode compatibility", job, StringComparison.Ordinal);
        Assert.Contains("compatible=true", job, StringComparison.Ordinal);
        Assert.Contains("compatible=false", job, StringComparison.Ordinal);
    }

    [Fact]
    public void CiWorkflow_WindowsJobBuildsExpectedFramework()
    {
        var job = ExtractJob("ci.yml", "windows-build");

        Assert.Contains("-f net10.0-windows10.0.19041.0", job, StringComparison.Ordinal);
    }

    [Fact]
    public void CiWorkflow_MacJobBuildsCatalystWithSigningDisabled()
    {
        var job = ExtractJob("ci.yml", "mac-build");

        Assert.Contains("-f net10.0-maccatalyst", job, StringComparison.Ordinal);
        // Mac Catalyst build intentionally disables signing on CI runners.
        Assert.Contains("-p:EnableCodeSigning=false", job, StringComparison.Ordinal);
        Assert.Contains("-p:CodesignRequireProvisioningProfile=false", job, StringComparison.Ordinal);
    }

    [Fact]
    public void DeliveryWorkflow_PackageJobPreservesSharedReleaseGuards()
    {
        var job = ExtractJob("delivery.yml", "package");

        // Release packaging shares the same GitVersion / SDK / workload / Xcode constraints,
        // all rooted inside the single matrix job.
        Assert.Contains("fetch-depth: 0", job, StringComparison.Ordinal);
        Assert.Contains("dotnet-version: 10.0.x", job, StringComparison.Ordinal);
        Assert.Contains("dotnet-quality: ga", job, StringComparison.Ordinal);
        Assert.Contains("dotnet workload install maui", job, StringComparison.Ordinal);
        Assert.DoesNotContain("--skip-manifest-update", job, StringComparison.Ordinal);
        Assert.Contains("sudo xcodebuild -runFirstLaunch", job, StringComparison.Ordinal);
    }

    [Fact]
    public void DeliveryWorkflow_WindowsMatrixEntry_LeavesRidEmpty()
    {
        var entry = ExtractMatrixEntry("delivery.yml", "windows");

        // Windows publish intentionally leaves the RID empty; reintroducing one caused Mono
        // runtime packaging failures in the past.
        Assert.Contains("rid: \"\"", entry, StringComparison.Ordinal);
        Assert.DoesNotContain("rid: win-", entry, StringComparison.Ordinal);
    }

    [Fact]
    public void DeliveryWorkflow_MacCatalystMatrixEntry_UsesMaccatalystRid()
    {
        var entry = ExtractMatrixEntry("delivery.yml", "maccatalyst");

        Assert.Contains("rid: maccatalyst-x64", entry, StringComparison.Ordinal);
        Assert.Contains("tfm: net10.0-maccatalyst", entry, StringComparison.Ordinal);
        Assert.Contains("EnableCodeSigning=false", entry, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns the body of a top-level job (under <c>jobs:</c>) so per-job
    /// invariants can be asserted without bleeding into sibling jobs.
    /// </summary>
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

        // The next sibling job header starts at 2-space indent + non-whitespace.
        var endIndex = lines.Length;
        for (var i = startIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Length >= 3 && line[0] == ' ' && line[1] == ' ' && line[2] != ' ' && line.TrimStart().StartsWith(' ') == false)
            {
                endIndex = i;
                break;
            }
        }

        return string.Join('\n', lines, startIndex, endIndex - startIndex);
    }

    /// <summary>
    /// Returns the lines of a single <c>matrix.include</c> entry keyed by
    /// <c>- name: {entryName}</c> so Windows/Mac-specific release invariants can
    /// be asserted independently.
    /// </summary>
    private static string ExtractMatrixEntry(string fileName, string entryName)
    {
        var workflow = ReadWorkflow(fileName);
        var lines = workflow.Split('\n');

        var header = $"          - name: {entryName}";
        var startIndex = Array.FindIndex(lines, l => l.TrimEnd('\r') == header);
        if (startIndex < 0)
        {
            throw new InvalidOperationException($"Matrix entry '{entryName}' not found in {fileName}.");
        }

        var endIndex = lines.Length;
        for (var i = startIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            // Next matrix entry or less-indented block ends this entry.
            if (line.StartsWith("          - ", StringComparison.Ordinal))
            {
                endIndex = i;
                break;
            }

            if (line.Length > 0 && !line.StartsWith("            ", StringComparison.Ordinal))
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
            if (File.Exists(Path.Combine(current.FullName, "Praxis.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test runtime base directory.");
    }
}
