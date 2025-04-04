using System.Linq;
using Nuke.Cola;
using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

public class Build : NukeBuild, IPublishNugets
{
    public static int Main() => Execute<Build>();
    protected override void OnBuildCreated() => NoLogo = true;

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
    NuGetPublishTarget[] PublishTo = [ IsLocalBuild ? NuGetPublishTarget.Github : NuGetPublishTarget.NugetOrg ];
    
    public string VersionForNuget => GitVersion.NuGetVersion;

    public ProjectRecord[] PublishProjects => [ MainProject ];

    public NugetSource[] NugetSources => [..
        NugetSource.CombineFrom(
            [.. PublishTo.Select(p => p.Source)],
            NugetApiKeys
        )
    ];

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
        .DependentFor<IPublishNugets>(_ => _.PublishNuget)
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
                    .SetConfiguration(Configuration)
                    .SetVersion(GitVersion.NuGetVersion)
                    .SetAssemblyVersion(GitVersion.MajorMinorPatch)
                );
            }
        });
}
