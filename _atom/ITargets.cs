namespace Atom;

[PublicAPI]
internal interface ITargets : ISetupBuildInfo, IDotnetPackHelper, IDotnetTestHelper, INugetHelper, IGithubReleaseHelper
{
    const string JsonExtensionsProjectName = "DecSm.Extensions.Json";
    const string JsonExtensionsTestProjectName = "DecSm.Extensions.Json.Tests";

    [ParamDefinition("nuget-push-feed", "The Nuget feed to push to.", "https://api.nuget.org/v3/index.json")]
    string NugetFeed => GetParam(() => NugetFeed, "https://api.nuget.org/v3/index.json");

    [SecretDefinition("nuget-push-api-key", "The API key to use to push to Nuget.")]
    string? NugetApiKey => GetParam(() => NugetApiKey);

    Target PackJsonExtensions =>
        d => d
            .DescribedAs("Builds the DecSm.Extensions.Json project into a NuGet package")
            .ProducesArtifact(JsonExtensionsProjectName)
            .Executes(async cancellationToken => await DotnetPackProject(new(JsonExtensionsProjectName), cancellationToken));

    Target TestJsonExtensions =>
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

    Target PushToNuget =>
        d => d
            .DescribedAs("Pushes the Atom projects to Nuget")
            .RequiresParam(nameof(NugetFeed))
            .RequiresParam(nameof(NugetApiKey))
            .ConsumesArtifact(nameof(PackJsonExtensions), JsonExtensionsProjectName)
            .Executes(async cancellationToken =>
                await PushProject(JsonExtensionsProjectName, NugetFeed, NugetApiKey!, cancellationToken: cancellationToken));

    Target PushToRelease =>
        d => d
            .DescribedAs("Pushes the package to the release feed.")
            .RequiresParam(nameof(GithubToken))
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
}
