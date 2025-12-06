using DecSm.Atom;
using DecSm.Atom.Build.Definition;
using DecSm.Atom.Hosting;
using DecSm.Atom.Module.Dotnet;
using DecSm.Atom.Module.GithubWorkflows;
using DecSm.Atom.Module.GithubWorkflows.Generation.Options;
using DecSm.Atom.Module.GitVersion;
using DecSm.Atom.Params;
using DecSm.Atom.Reports;
using DecSm.Atom.Workflows.Definition;
using DecSm.Atom.Workflows.Definition.Options;
using DecSm.Atom.Workflows.Definition.Triggers;
using DecSm.Atom.Workflows.Options;

namespace Atom;

[BuildDefinition]
[GenerateEntryPoint]
[GenerateInterfaceMembers]
internal partial class Build : DefaultBuildDefinition,
    IGithubWorkflows,
    IGitVersion,
    IDotnetPackHelper,
    IDotnetTestHelper,
    INugetHelper,
    IGithubReleaseHelper
{
    private const string JsonExtensionsProjectName = "DecSm.Extensions.Json";
    private const string JsonExtensionsTestProjectName = "DecSm.Extensions.Json.Tests";
    private const string JsonExtensionsBenchmarkProjectName = "DecSm.Extensions.Json.Benchmarks";

    [ParamDefinition("nuget-push-feed", "The Nuget feed to push to.", "https://api.nuget.org/v3/index.json")]
    private string NugetFeed => GetParam(() => NugetFeed, "https://api.nuget.org/v3/index.json");

    [SecretDefinition("nuget-push-api-key", "The API key to use to push to Nuget.")]
    private string? NugetApiKey => GetParam(() => NugetApiKey);

    private Target PackJsonExtensions =>
        d => d
            .DescribedAs("Builds the DecSm.Extensions.Json project into a NuGet package")
            .ProducesArtifact(JsonExtensionsProjectName)
            .Executes(async cancellationToken =>
                await DotnetPackProject(new(JsonExtensionsProjectName), cancellationToken));

    private Target TestJsonExtensions =>
        d => d
            .DescribedAs("Runs the DecSm.Extensions.Json.Tests tests")
            .ProducesArtifact(JsonExtensionsTestProjectName)
            .Executes(async cancellationToken =>
            {
                var exitCode = 0;

                exitCode += await RunDotnetUnitTests(new(JsonExtensionsTestProjectName), cancellationToken);

                if (exitCode != 0)
                    throw new StepFailedException("One or more unit tests failed");
            });

    private Target BenchmarkJsonExtensions =>
        d => d
            .DescribedAs("Runs the DecSm.Extensions.Json.Benchmarks benchmarks")
            .ProducesArtifact(JsonExtensionsBenchmarkProjectName)
            .Executes(async cancellationToken =>
            {
                var benchmarkPublishDirectory = FileSystem.AtomPublishDirectory / JsonExtensionsBenchmarkProjectName;

                if (benchmarkPublishDirectory.DirectoryExists)
                    FileSystem.Directory.Delete(benchmarkPublishDirectory, true);

                FileSystem.Directory.CreateDirectory(benchmarkPublishDirectory);

                var buildResult = await ProcessRunner.RunAsync(new("dotnet",
                        $"run --project {JsonExtensionsBenchmarkProjectName} --configuration Release")
                    {
                        WorkingDirectory = FileSystem.AtomRootDirectory,
                    },
                    cancellationToken);

                if (buildResult.ExitCode != 0)
                    throw new StepFailedException($"One or more benchmarks failed: {buildResult.Error}");

                foreach (var reportFile in FileSystem.Directory.GetFiles(
                             FileSystem.AtomRootDirectory / "BenchmarkDotNet.Artifacts" / "results",
                             "DecSm.Extensions.Json.Benchmarks.JsonUtilBenchmarks-report*"))
                    FileSystem.File.Copy(reportFile,
                        benchmarkPublishDirectory /
                        FileSystem.FileInfo.New(reportFile)
                            .Name);

                var markdownReportText = await FileSystem.File.ReadAllTextAsync(
                    benchmarkPublishDirectory / "DecSm.Extensions.Json.Benchmarks.JsonUtilBenchmarks-report-github.md",
                    cancellationToken);

                AddReportData(new TextReportData(markdownReportText)
                {
                    Title = "JsonUtil Benchmarks",
                });

                FileSystem.Directory.Delete(FileSystem.AtomRootDirectory / "BenchmarkDotNet.Artifacts", true);
            });

    private Target PushToNuget =>
        d => d
            .DescribedAs("Pushes the Atom projects to Nuget")
            .RequiresParam(nameof(NugetFeed))
            .RequiresParam(nameof(NugetApiKey))
            .ConsumesArtifact(nameof(PackJsonExtensions), JsonExtensionsProjectName)
            .Executes(async cancellationToken => await PushProject(JsonExtensionsProjectName,
                NugetFeed,
                NugetApiKey!,
                null,
                cancellationToken));

    private Target PushToRelease =>
        d => d
            .DescribedAs("Pushes the package to the release feed.")
            .RequiresParam(nameof(IGithubReleaseHelper.GithubToken))
            .ConsumesVariable(nameof(SetupBuildInfo), nameof(BuildVersion))
            .ConsumesArtifact(nameof(PackJsonExtensions), JsonExtensionsProjectName)
            .Executes(async () =>
            {
                if (BuildVersion.IsPreRelease)
                {
                    Logger.LogInformation("Skipping release push for pre-release version");

                    return;
                }

                await UploadArtifactToRelease(JsonExtensionsProjectName, $"v{BuildVersion}");
            });

    public override IReadOnlyList<IWorkflowOption> GlobalWorkflowOptions =>
    [
        UseGitVersionForBuildId.Enabled, new SetupDotnetStep("9.0.x"),
    ];

    public override IReadOnlyList<WorkflowDefinition> Workflows =>
    [
        new("Validate")
        {
            Triggers = [GitPullRequestTrigger.IntoMain, ManualTrigger.Empty],
            Targets =
            [
                Targets.SetupBuildInfo,
                Targets.PackJsonExtensions.WithSuppressedArtifactPublishing,
                Targets.TestJsonExtensions.WithSuppressedArtifactPublishing,
                Targets.BenchmarkJsonExtensions.WithSuppressedArtifactPublishing,
            ],
            WorkflowTypes = [Github.WorkflowType],
        },
        new("Build")
        {
            Triggers = [GitPushTrigger.ToMain, GithubReleaseTrigger.OnReleased, ManualTrigger.Empty],
            Targets =
            [
                Targets.SetupBuildInfo,
                Targets.PackJsonExtensions,
                Targets.TestJsonExtensions,
                Targets.BenchmarkJsonExtensions,
                Targets.PushToNuget.WithOptions(WorkflowSecretInjection.Create(Params.NugetApiKey)),
                Targets
                    .PushToRelease
                    .WithGithubTokenInjection()
                    .WithOptions(GithubIf.Create(new ConsumedVariableExpression(nameof(Targets.SetupBuildInfo),
                            ParamDefinitions[nameof(BuildVersion)].ArgName)
                        .Contains(new StringExpression("-"))
                        .EqualTo("false"))),
            ],
            WorkflowTypes = [Github.WorkflowType],
        },
        Github.DependabotDefaultWorkflow(),
    ];
}
