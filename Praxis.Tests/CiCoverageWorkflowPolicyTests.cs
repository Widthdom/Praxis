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
        var steps = ParseSteps(ExtractJob("ci.yml", "mac-build"));

        // Structural: Initialize Xcode must actually run -runFirstLaunch (not just mention it in a comment).
        var firstLaunch = steps.Single(s => s.Name == "Initialize Xcode");
        Assert.Contains("sudo xcodebuild -runFirstLaunch", firstLaunch.Run ?? string.Empty, StringComparison.Ordinal);

        // Structural: Check Xcode compatibility owns the id used downstream and emits both gate outputs.
        var xcodeCheck = steps.Single(s => s.Name == "Check Xcode compatibility");
        Assert.Equal("xcode_check", xcodeCheck.Id);
        Assert.Contains("compatible=true", xcodeCheck.Run ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("compatible=false", xcodeCheck.Run ?? string.Empty, StringComparison.Ordinal);

        // Structural: Mac Catalyst build must be guarded by the compatibility check output.
        var build = steps.Single(s => s.Name == "Build app (Mac Catalyst)");
        Assert.Equal("steps.xcode_check.outputs.compatible == 'true'", build.If);
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
        var steps = ParseSteps(job);

        // Non-matrix-specific shared guards remain string-level because they target a
        // single unguarded step (checkout/setup-dotnet/workload install).
        Assert.Contains("fetch-depth: 0", job, StringComparison.Ordinal);
        Assert.Contains("dotnet-version: 10.0.x", job, StringComparison.Ordinal);
        Assert.Contains("dotnet-quality: ga", job, StringComparison.Ordinal);
        Assert.DoesNotContain("--skip-manifest-update", job, StringComparison.Ordinal);

        var installWorkload = steps.Single(s => s.Name == "Install MAUI workload");
        Assert.Equal("dotnet workload install maui", (installWorkload.Run ?? string.Empty).Trim());
        Assert.Null(installWorkload.If);

        // Structural: Initialize Xcode must be gated to the maccatalyst matrix entry only —
        // a regression that drops or mis-scopes the `if:` would make Windows runners try xcodebuild.
        var initXcode = steps.Single(s => s.Name == "Initialize Xcode");
        Assert.Equal("matrix.name == 'maccatalyst'", initXcode.If);
        Assert.Contains("sudo xcodebuild -runFirstLaunch", initXcode.Run ?? string.Empty, StringComparison.Ordinal);

        var xcodeCheck = steps.Single(s => s.Name == "Check Xcode compatibility (Mac Catalyst)");
        Assert.Equal("xcode_check", xcodeCheck.Id);
        Assert.Equal("matrix.name == 'maccatalyst'", xcodeCheck.If);

        var publish = steps.Single(s => s.Name == "Publish app");
        Assert.Equal("matrix.name != 'maccatalyst' || steps.xcode_check.outputs.compatible == 'true'", publish.If);
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

    private sealed record WorkflowStep(string? Name, string? If, string? Run, string? Uses, string? Id);

    /// <summary>
    /// Lightweight structural parser for GitHub Actions steps inside a job body.
    /// Returns top-level step fields (<c>name</c>, <c>if</c>, <c>run</c>, <c>uses</c>,
    /// <c>id</c>) so tests can assert step-name + adjacent-field invariants rather
    /// than raw substring presence, which would also match comments, echoed shell
    /// text, or mis-scoped <c>if:</c> conditions.
    /// </summary>
    private static List<WorkflowStep> ParseSteps(string jobBody)
    {
        const int StepIndent = 6;  // "      - name:"
        const int FieldIndent = 8; // "        name:"

        var lines = jobBody.Replace("\r", string.Empty).Split('\n');
        var steps = new List<WorkflowStep>();

        var i = Array.FindIndex(lines, l => l.TrimEnd() == "    steps:");
        if (i < 0) return steps;
        i++;

        while (i < lines.Length)
        {
            var line = lines[i];
            if (LeadingSpaces(line) < StepIndent && line.Trim().Length > 0)
            {
                break;
            }

            if (line.StartsWith("      - ", StringComparison.Ordinal))
            {
                var stepLines = new List<string> { line };
                var start = i;
                i++;
                while (i < lines.Length)
                {
                    var l = lines[i];
                    if (l.StartsWith("      - ", StringComparison.Ordinal)) break;
                    if (l.Trim().Length > 0 && LeadingSpaces(l) < StepIndent) break;
                    stepLines.Add(l);
                    i++;
                }

                steps.Add(ParseStep(stepLines, FieldIndent));
            }
            else
            {
                i++;
            }
        }

        return steps;
    }

    private static WorkflowStep ParseStep(List<string> stepLines, int fieldIndent)
    {
        string? name = null, ifCond = null, run = null, uses = null, id = null;

        // Normalize first line: "      - name: Foo" → treat as field at fieldIndent.
        var first = stepLines[0];
        var firstAfterDash = first.Substring(8); // skip "      - "
        var normalized = new List<string> { new string(' ', fieldIndent) + firstAfterDash };
        for (var k = 1; k < stepLines.Count; k++) normalized.Add(stepLines[k]);

        for (var k = 0; k < normalized.Count; k++)
        {
            var line = normalized[k];
            if (line.Trim().Length == 0) continue;
            if (LeadingSpaces(line) != fieldIndent) continue;

            var trimmed = line.TrimStart();
            var colon = trimmed.IndexOf(':');
            if (colon < 0) continue;

            var key = trimmed.Substring(0, colon);
            var rest = trimmed.Substring(colon + 1).TrimStart();

            switch (key)
            {
                case "name": name = UnquoteInline(rest); break;
                case "if": ifCond = UnquoteInline(rest); break;
                case "uses": uses = UnquoteInline(rest); break;
                case "id": id = UnquoteInline(rest); break;
                case "run":
                    run = CaptureScalar(rest, normalized, ref k, fieldIndent);
                    break;
            }
        }

        return new WorkflowStep(name, ifCond, run, uses, id);
    }

    private static string CaptureScalar(string rest, List<string> lines, ref int index, int fieldIndent)
    {
        if (rest.Length == 0 || rest[0] == '|' || rest[0] == '>')
        {
            var body = new System.Text.StringBuilder();
            var childIndent = fieldIndent + 2;
            for (var k = index + 1; k < lines.Count; k++)
            {
                var l = lines[k];
                if (l.Trim().Length == 0)
                {
                    body.AppendLine();
                    index = k;
                    continue;
                }
                if (LeadingSpaces(l) < childIndent) break;
                body.AppendLine(l.Substring(childIndent));
                index = k;
            }
            return body.ToString().TrimEnd('\n');
        }

        return UnquoteInline(rest);
    }

    private static string UnquoteInline(string value)
    {
        var v = value.Trim();
        if (v.Length >= 2 && ((v[0] == '"' && v[^1] == '"') || (v[0] == '\'' && v[^1] == '\'')))
        {
            v = v.Substring(1, v.Length - 2);
        }
        return v;
    }

    private static int LeadingSpaces(string line)
    {
        var n = 0;
        while (n < line.Length && line[n] == ' ') n++;
        return n;
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
