namespace Atom;

internal interface ITargets : IDotnetPackHelper, IDotnetTestHelper, INugetHelper, IGithubReleaseHelper, ISetupBuildInfo
{
    static readonly string[] ProjectsToPack = [Projects.DecSm_Extensions_Json.Name];
    static readonly string[] ProjectsToTest = [Projects.DecSm_Extensions_Json_Tests.Name];

    [ParamDefinition("test-framework", "Test framework to use for unit tests")]
    string TestFramework => GetParam(() => TestFramework, "net10.0");

    [ParamDefinition("nuget-push-feed", "The Nuget feed to push to.")]
    string NugetFeed => GetParam(() => NugetFeed, "https://api.nuget.org/v3/index.json");

    [SecretDefinition("nuget-push-api-key", "The API key to use to push to Nuget.")]
    string? NugetApiKey => GetParam(() => NugetApiKey);

    Target Pack =>
        t => t
            .DescribedAs("Packs NuGet packages")
            .ProducesArtifacts(ProjectsToPack)
            .Executes(async cancellationToken =>
            {
                foreach (var projectName in ProjectsToPack)
                    await DotnetPackAndStage(projectName, cancellationToken: cancellationToken);
            });

    Target Test =>
        d => d
            .DescribedAs("Runs all unit tests")
            .RequiresParam(nameof(TestFramework))
            .ProducesArtifacts(ProjectsToTest)
            .Executes(async cancellationToken =>
            {
                var exitCode = 0;

                foreach (var projectName in ProjectsToTest)
                    exitCode += await DotnetTestAndStage(projectName,
                        new()
                        {
                            TestOptions = new()
                            {
                                Framework = TestFramework,
                            },
                        },
                        cancellationToken);

                if (exitCode != 0)
                    throw new StepFailedException("One or more unit tests failed");
            });

    Target PushToNuget =>
        d => d
            .DescribedAs("Pushes packages to Nuget")
            .RequiresParam(nameof(NugetFeed), nameof(NugetApiKey))
            .ConsumesArtifacts(nameof(Pack), ProjectsToPack)
            .DependsOn(nameof(Test))
            .Executes(async cancellationToken =>
            {
                foreach (var projectName in ProjectsToPack)
                    await PushProject(projectName, NugetFeed, NugetApiKey!, cancellationToken: cancellationToken);
            });

    Target PushToRelease =>
        d => d
            .DescribedAs("Pushes artifacts to a GitHub release")
            .RequiresParam(nameof(GithubToken))
            .ConsumesVariable(nameof(SetupBuildInfo), nameof(BuildVersion))
            .ConsumesArtifacts(nameof(Pack), ProjectsToPack)
            .ConsumesArtifacts(nameof(Test),
                ProjectsToTest,
                PlatformNames.SelectMany(platform => FrameworkNames.Select(framework => $"{platform}-{framework}")))
            .Executes(async () =>
            {
                foreach (var projectName in ProjectsToPack.Concat(ProjectsToTest))
                    await UploadArtifactToRelease(projectName, $"v{BuildVersion}");
            });
}
