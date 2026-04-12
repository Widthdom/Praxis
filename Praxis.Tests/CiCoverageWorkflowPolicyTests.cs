namespace Praxis.Tests;

public class CiCoverageWorkflowPolicyTests
{
    [Fact]
    public void CiWorkflow_CollectsAndUploadsCoberturaCoverage()
    {
        var root = ResolveRepositoryRoot();
        var workflowPath = Path.Combine(root, ".github", "workflows", "ci.yml");
        var workflow = File.ReadAllText(workflowPath);

        Assert.Contains("--collect:\"XPlat Code Coverage\"", workflow, StringComparison.Ordinal);
        Assert.Contains("--results-directory ./TestResults", workflow, StringComparison.Ordinal);
        Assert.Contains("name: Upload coverage artifact", workflow, StringComparison.Ordinal);
        Assert.Contains("path: TestResults/**/coverage.cobertura.xml", workflow, StringComparison.Ordinal);
        Assert.Contains("name: praxis-test-coverage-cobertura", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void CiWorkflow_UsesFullHistoryCheckoutForGitVersioning()
    {
        var workflow = ReadWorkflow("ci.yml");

        // Nerdbank.GitVersioning needs history for version-height computation.
        Assert.Contains("fetch-depth: 0", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("fetch-depth: 1", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void CiWorkflow_PinsDotnetSdkToGaChannel()
    {
        var workflow = ReadWorkflow("ci.yml");

        Assert.Contains("dotnet-version: 10.0.x", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet-quality: ga", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void CiWorkflow_InstallsMauiWorkloadWithoutSkipManifestUpdate()
    {
        var workflow = ReadWorkflow("ci.yml");

        Assert.Contains("dotnet workload install maui", workflow, StringComparison.Ordinal);
        // --skip-manifest-update was explicitly removed; do not reintroduce it casually.
        Assert.DoesNotContain("--skip-manifest-update", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void CiWorkflow_RunsXcodeFirstLaunchAndGuardsCompatibility()
    {
        var workflow = ReadWorkflow("ci.yml");

        // Mac Catalyst runs need -runFirstLaunch before build.
        Assert.Contains("sudo xcodebuild -runFirstLaunch", workflow, StringComparison.Ordinal);
        // The Xcode floor gate exists to skip Mac Catalyst when Xcode is too old.
        Assert.Contains("Check Xcode compatibility", workflow, StringComparison.Ordinal);
        Assert.Contains("compatible=true", workflow, StringComparison.Ordinal);
        Assert.Contains("compatible=false", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void CiWorkflow_BuildsExpectedPlatformFrameworks()
    {
        var workflow = ReadWorkflow("ci.yml");

        Assert.Contains("-f net10.0-windows10.0.19041.0", workflow, StringComparison.Ordinal);
        Assert.Contains("-f net10.0-maccatalyst", workflow, StringComparison.Ordinal);
        // Mac Catalyst build intentionally disables signing on CI runners.
        Assert.Contains("-p:EnableCodeSigning=false", workflow, StringComparison.Ordinal);
        Assert.Contains("-p:CodesignRequireProvisioningProfile=false", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void DeliveryWorkflow_PreservesCriticalReleaseGuards()
    {
        var workflow = ReadWorkflow("delivery.yml");

        // Release packaging shares the same GitVersion / SDK / workload / Xcode constraints.
        Assert.Contains("fetch-depth: 0", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet-quality: ga", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet workload install maui", workflow, StringComparison.Ordinal);
        Assert.Contains("sudo xcodebuild -runFirstLaunch", workflow, StringComparison.Ordinal);
        // Windows publish intentionally leaves the RID empty; reintroducing one caused Mono
        // runtime packaging failures in the past.
        Assert.Contains("rid: \"\"", workflow, StringComparison.Ordinal);
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
