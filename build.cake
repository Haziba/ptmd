#addin "Cake.Docker&version=0.10.0"
#addin "Cake.FileHelpers&version=3.2.0"
#addin nuget:?package=AWSSDK.CloudFormation&version=3.3.100.7&loaddependencies=true
#addin nuget:?package=AWSSDK.DynamoDBv2&version=3.3.100.7&loaddependencies=true
#addin nuget:?package=AWSSDK.Kinesis&version=3.3.100.7
#addin nuget:?package=AWSSDK.SecurityToken&version=3.3.100.7&loaddependencies=true
#addin nuget:?package=AWSSDK.SimpleSystemsManagement&version=3.3.100.8
using System.Text.RegularExpressions;
using System.Threading;
using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

var iamStackName = "ptmd-backend-iam";

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

var sourceDir = Directory("./src");
var solutions = GetFiles("./**/*.sln");

// BUILD OUTPUT DIRECTORIES
var publishDir = Directory("./publish/");
var artifactsDir = Directory("./artifacts/");

// VERBOSITY
var dotNetCoreVerbosity = Cake.Common.Tools.DotNetCore.DotNetCoreVerbosity.Quiet;

AWSCredentials localAwsCredentials = null;
var dockerComposeFile = "./support/docker-compose.yml";

///////////////////////////////////////////////////////////////////////////////
// COMMON FUNCTION DEFINITIONS
///////////////////////////////////////////////////////////////////////////////

string GetProjectName(string project)
{
    return project
        .Split(new [] {'/'}, StringSplitOptions.RemoveEmptyEntries)
        .Last()
        .Replace(".csproj", string.Empty);
}

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    // Executed BEFORE the first task.
    EnsureDirectoryExists(publishDir);
    EnsureDirectoryExists(artifactsDir);
    Information("Running tasks...");
});
Teardown(ctx =>
{
    // Executed AFTER the last task.
    Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASK DEFINITIONS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .Description("Cleans all directories that are used during the build process.")
    .Does(() =>
    {
        foreach(var solution in solutions)
        {
            Information("Cleaning {0}", solution.FullPath);
            CleanDirectories(solution.FullPath + "/**/bin/" + configuration);
            CleanDirectories(solution.FullPath + "/**/obj/" + configuration);
            Information("{0} was clean.", solution.FullPath);
        }

        CleanDirectory(publishDir);
        CleanDirectory(artifactsDir);
    });

Task("Restore")
    .Description("Restores all the NuGet packages that are used by the specified solution.")
    .Does(() =>
    {
        var settings = new DotNetCoreRestoreSettings
        {
            DisableParallel = false,
            NoCache = true,
            Verbosity = dotNetCoreVerbosity
        };

        foreach(var solution in solutions)
        {
            Information("Restoring NuGet packages for '{0}'...", solution);
            DotNetCoreRestore(solution.FullPath, settings);
            Information("NuGet packages restored for '{0}'.", solution);
        }
    });

Task("Build")
    .Description("Builds all the different parts of the project.")
    .Does(() =>
    {
        var msBuildSettings = new DotNetCoreMSBuildSettings
        {
            TreatAllWarningsAs = MSBuildTreatAllWarningsAs.Error,
            Verbosity = dotNetCoreVerbosity
        };

        var settings = new DotNetCoreBuildSettings
        {
            Configuration = configuration,
            MSBuildSettings = msBuildSettings,
            NoRestore = true
        };

        foreach(var solution in solutions)
        {
            Information("Building '{0}'...", solution);
            DotNetCoreBuild(solution.FullPath, settings);
            Information("'{0}' has been built.", solution);
        }
    });

Task("Test-Unit")
    .Description("Runs all unit tests.")
    .Does(() =>
    {
        TestInParallel(GetFiles("./test/**/*.csproj"));
    });

public void TestInParallel(
    FilePathCollection files,
    int maxDegreeOfParallelism = -1,
    CancellationToken cancellationToken = default(CancellationToken))
{
    var settings = new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoRestore = true,
        NoBuild = true,
        Logger = "trx",
        ResultsDirectory = artifactsDir,
        Verbosity = DotNetCoreVerbosity.Minimal
    };

    var actions = new List<Action>();
    foreach (var file in files) {
        actions.Add(() => {
            Information("Testing '{0}'...", file);
            DotNetCoreTest(file.FullPath, settings);
            Information("'{0}' has been tested.", file);
        });
    }

    var options = new ParallelOptions {
        MaxDegreeOfParallelism = maxDegreeOfParallelism,
        CancellationToken = cancellationToken
    };

    Parallel.Invoke(options, actions.ToArray());
}

Task("Publish")
    .Description("Publish the Projects.")
    .DoesForEach(GetFiles("./src/**/*.csproj"), project =>
    {
        if (XmlPeek(project.FullPath, "//PropertyGroup/TargetFramework").StartsWith("netstandard"))
            return;

        var projectName = GetProjectName(project.FullPath);
        Information("Publishing '{0}'...", projectName);

        var outputDirectory = publishDir + Directory(projectName);

        var msBuildSettings = new DotNetCoreMSBuildSettings
        {
            TreatAllWarningsAs = MSBuildTreatAllWarningsAs.Error,
            Verbosity = dotNetCoreVerbosity
        };

        var settings = new DotNetCorePublishSettings
        {
            Configuration = configuration,
            MSBuildSettings = msBuildSettings,
            NoRestore = true,
            OutputDirectory = outputDirectory,
            Runtime = "linux-x64",
            Verbosity = dotNetCoreVerbosity
        };
        DotNetCorePublish(project.FullPath, settings);

        Information("'{0}' has been published.", projectName);
    });

Task("Local-AWS")
    .Description("Adds dependent resources into a local AWS substitute (localstack)")
    .Does(async () =>
    {
        Information("Ensuring AWS Profile is defined");
        EnsureAwsProfile();

        Information("Ensuring Support Stack is running");
        DockerComposeUp(new DockerComposeUpSettings
        {
            Files = new [] { dockerComposeFile },
            DetachedMode = true,
            RemoveOrphans = true
        });

        Information("Configured Local AWS");
    });

void EnsureAwsProfile()
{
    var profileName = "localstack";
    CredentialProfile awsProfile;
    var credentialStore = new SharedCredentialsFile();
    if (!credentialStore.TryGetProfile(profileName, out awsProfile))
    {
        var options = new CredentialProfileOptions
        {
            AccessKey = "localstack",
            SecretKey = "localstack"
        };
        awsProfile = new CredentialProfile(profileName, options) {Region = RegionEndpoint.EUWest1};
        credentialStore.RegisterProfile(awsProfile);
        Information($"Profile: '{profileName}' has been created");
    }
    localAwsCredentials = awsProfile.GetAWSCredentials(credentialStore);
}

async Task EnsureParameter(PutParameterRequest request)
{
    using (var ssm = new AmazonSimpleSystemsManagementClient(localAwsCredentials, new AmazonSimpleSystemsManagementConfig
    {
        ServiceURL = "http://localhost:4583",
        AuthenticationRegion = RegionEndpoint.EUWest1.SystemName
    }))
    {
        var response = await ssm.PutParameterAsync(request);
        Information("Parameter: {0} \t Value: {1} \t Version: {2}",
                    request.Name,
                    request.Value,
                    response.Version);
    }
}

Task("Local-AWS-Down")
    .Description("Removes dependent resources into a local AWS substitute (localstack)")
    .Does(CleanupEnvironment);

void CleanupEnvironment() {
    Information("Shutting down Support Stack");
    DockerComposeDown(new DockerComposeDownSettings{
        Files = new [] { dockerComposeFile }
    });
}

Task("Test-E2E")
    .Description("Runs end-to-end tests.")
    .IsDependentOn("Local-AWS")
    .Does(() =>
    {
        var settings = new DotNetCoreTestSettings
        {
            Configuration = configuration,
            NoRestore = true,
            NoBuild = true,
            Logger = "trx",
            ResultsDirectory = artifactsDir,
            Verbosity = DotNetCoreVerbosity.Minimal
        };

        var projectFiles = GetFiles("./e2e/**/*.csproj");
        foreach(var file in projectFiles)
        {
            Information("Testing '{0}'...", file);
            DotNetCoreTest(file.FullPath, settings);
            Information("'{0}' has been tested.", file);
        }
    });

Task("Update-IAM")
    .Description("Updates the IAM Roles for a specified Account")
    .Does(async () =>
    {
        var client = GetCloudFormationClient();
        var response = await client.UpdateStackAsync(new UpdateStackRequest
        {
            Capabilities = new List<string> { "CAPABILITY_IAM" },
            StackName = iamStackName,
            TemplateBody = System.IO.File.ReadAllText("./template_iam.yaml")
        });
        Information(la => la("IAM update result {0}, StackId: {1}", response.HttpStatusCode, response.StackId));
    });

IAmazonCloudFormation GetCloudFormationClient()
{
    var profileName = Argument("profile", "dev");
    var credentialStore = new CredentialProfileStoreChain();
    if (!credentialStore.TryGetProfile(profileName, out var profile))
        throw new Exception($"Could not find AWS Profile, no profile named '{profileName}'");
    var credentials = AWSCredentialsFactory.GetAWSCredentials(profile, credentialStore);
    var assumeRoleCredentials = credentials as AssumeRoleAWSCredentials;
    if (assumeRoleCredentials != null)
        assumeRoleCredentials.Options.MfaTokenCodeCallback = () => {
            Console.Write($"Enter MFA code for '{profileName}': ");
            var code = string.Empty;
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    code += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && code.Length > 0)
                    {
                        code = code.Substring(0, (code.Length - 1));
                        Console.Write("\b \b");
                    }
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return code;
        };
    var region = Argument("region", "eu-west-1");
    return new AmazonCloudFormationClient(credentials, RegionEndpoint.GetBySystemName(region));
}

///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Package")
    .Description("This is the task which will run if target Package is passed in.")
    .IsDependentOn("Default")
    .IsDependentOn("Test-Unit")
    .IsDependentOn("Publish")
    .Does(() => { Information("Package target ran."); });

Task("Test")
    .Description("Runs just the unit tests.")
    .IsDependentOn("Default")
    .IsDependentOn("Test-Unit")
    .Does(() => { Information("Test target ran."); });

Task("Test-All")
    .Description("Runs all tests in solution.")
    .IsDependentOn("Package")
    .IsDependentOn("Test-E2E")
    .Does(() => { Information("Test-All target ran."); })
    .Finally(CleanupEnvironment);

Task("Default")
    .Description("This is the default task which will run if no specific target is passed in.")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
