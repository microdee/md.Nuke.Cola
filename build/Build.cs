using System;
using System.Linq;
using System.Reflection;
using Nuke.Cola;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;

public class Build : NukeBuild, IPublishNugets
{
    public static int Main() => Execute<Build>();
    protected override void OnBuildCreated() => NoLogo = true;

    const string MasterBranch = "master";

    [Solution]
    readonly Solution Solution;
    
    [GitVersion]
    readonly GitVersion GitVersion;
    
    ProjectRecord MainProject => new (Solution.GetProject("md.Nuke.Cola") , true);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    
    [Parameter]
    string[] NugetApiKeys;

    [Parameter]
    NuGetPublishTarget[] PublishTo = new [] { IsLocalBuild ? NuGetPublishTarget.Github : NuGetPublishTarget.NugetOrg };
    
    public string VersionForNuget => GitVersion.NuGetVersion;

    public ProjectRecord[] PublishProjects => new ProjectRecord[] { MainProject };

    public NugetSource[] NugetSources => NugetSource.CombineFrom(
        PublishTo.Select(p => p.ToString()).ToArray(),
        NugetApiKeys
    ).ToArray();

    Target Info => _ => _
        .Description("Print information about the current state of the environment")
        .Executes(() =>
        {
            Log.Information("GitVersion: {0}", GitVersion.FullSemVer);
            Log.Information("NugetVersion: {0}", GitVersion.NuGetVersion);
            Log.Information("NugetVersionV2: {0}", GitVersion.NuGetVersionV2);
            foreach(var project in PublishProjects)
            {
                Log.Information(project.Project.Name);
            }
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            foreach(var project in PublishProjects)
            {
                DotNetRestore(s => s
                    .SetProjectFile(project.Project)
                );
            }
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            foreach(var (project, _) in PublishProjects)
            {
                var projectMsBuild = project.GetMSBuildProject();
                projectMsBuild.SetProperty("Version", GitVersion.NuGetVersion);
                projectMsBuild.Save();

                DotNetBuild(s => s
                    .SetNoRestore(true)
                    .SetProjectFile(project)
                    .SetVersion(GitVersion.NuGetVersion)
                    .SetAssemblyVersion(GitVersion.MajorMinorPatch)
                );
            }
        });
}
