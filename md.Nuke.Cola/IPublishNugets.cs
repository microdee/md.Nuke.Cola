using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;

namespace Nuke.Cola;

public record ProjectRecord(Project Project, bool PublishToNuget);
public record NugetSource(string Source, string ApiKey)
{
    public static IEnumerable<NugetSource> CombineFrom(string[] sources, string[] apiKeys)
    {
        for (int i = 0; i < sources.Length; i++)
        {
            yield return new(sources[i], apiKeys[i % apiKeys.Length]);
        }
    }
}

public interface IPublishNugets : INukeBuild
{
    string VersionForNuget { get; }
    ProjectRecord[] PublishProjects { get; }
    NugetSource[] NugetSources { get; }

    Target PublishNuget => _ => _
        .Executes(() =>
        {
            Assert.NotNull(VersionForNuget);

            foreach (var (project, publish) in PublishProjects)
            {
                var msBuildProject = project.GetMSBuildProject();
                msBuildProject.SetProperty("Version", VersionForNuget);
                msBuildProject.Save();

                var outDirectory = project.Directory / ".nupkg";

                DotNetTasks.DotNetPack(_ => _
                    .SetProject(project)
                    .SetOutputDirectory(outDirectory)
                );

                var packageId = project.GetProperty("PackageId");
                var nupkgSymbols = outDirectory / $"{packageId}.{VersionForNuget}.symbols.nupkg";
                
                var nupkg = nupkgSymbols.FileExists()
                    ? nupkgSymbols
                    : outDirectory / $"{packageId}.{VersionForNuget}.nupkg";
                
                foreach (var (source, apiKey) in NugetSources)
                {
                    DotNetTasks.DotNetNuGetPush(s => s
                        .SetTargetPath(outDirectory / nupkg)
                        .SetApiKey(apiKey)
                        .SetSource(source)
                    );
                }
            }
        });
}