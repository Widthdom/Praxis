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
