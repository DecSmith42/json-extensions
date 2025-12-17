namespace Atom;

[BuildDefinition]
[GenerateEntryPoint]
[GenerateSolutionModel]
internal partial class Build : BuildDefinition, IGithubWorkflows, IGitVersion, ITargets
{
    public static readonly string[] PlatformNames =
    [
        IJobRunsOn.WindowsLatestTag, IJobRunsOn.UbuntuLatestTag, IJobRunsOn.MacOsLatestTag,
    ];

    public static readonly string[] FrameworkNames = ["net8.0", "net9.0", "net10.0"];

    private static readonly MatrixDimension TestFrameworkMatrix = new(nameof(ITargets.TestFramework))
    {
        Values = FrameworkNames,
    };

    public override IReadOnlyList<IWorkflowOption> GlobalWorkflowOptions =>
    [
        UseGitVersionForBuildId.Enabled, new SetupDotnetStep("10.0.x"),
    ];

    public override IReadOnlyList<WorkflowDefinition> Workflows =>
    [
        new("Validate")
        {
            Triggers = [ManualTrigger.Empty, GitPullRequestTrigger.IntoMain],
            Targets =
            [
                WorkflowTargets.SetupBuildInfo.WithSuppressedArtifactPublishing,
                WorkflowTargets.Pack.WithSuppressedArtifactPublishing,
                WorkflowTargets
                    .Test
                    .WithSuppressedArtifactPublishing
                    .WithGithubRunnerMatrix(PlatformNames)
                    .WithMatrixDimensions(TestFrameworkMatrix)
                    .WithOptions(new SetupDotnetStep("8.0.x"), new SetupDotnetStep("9.0.x")),
            ],
            WorkflowTypes = [Github.WorkflowType],
        },
        new("Build")
        {
            Triggers =
            [
                ManualTrigger.Empty,
                new GitPushTrigger
                {
                    IncludedBranches = ["main", "feature/**", "patch/**"],
                },
                GithubReleaseTrigger.OnReleased,
            ],
            Targets =
            [
                WorkflowTargets.SetupBuildInfo,
                WorkflowTargets.Pack,
                WorkflowTargets
                    .Test
                    .WithGithubRunnerMatrix(PlatformNames)
                    .WithMatrixDimensions(TestFrameworkMatrix)
                    .WithOptions(new SetupDotnetStep("8.0.x"), new SetupDotnetStep("9.0.x")),
                WorkflowTargets.PushToNuget.WithOptions(WorkflowSecretInjection.Create(Params.NugetApiKey)),
                WorkflowTargets
                    .PushToRelease
                    .WithGithubTokenInjection()
                    .WithOptions(GithubIf.Create(new ConsumedVariableExpression(nameof(ISetupBuildInfo.SetupBuildInfo),
                            ParamDefinitions[nameof(ISetupBuildInfo.BuildVersion)].ArgName)
                        .Contains(new StringExpression("-"))
                        .EqualTo("false"))),
            ],
            WorkflowTypes = [Github.WorkflowType],
        },
        Github.DependabotDefaultWorkflow(),
    ];
}
