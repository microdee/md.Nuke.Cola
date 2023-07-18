using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;

namespace Nuke.Cola.BuildPlugins;

internal static class DotnetCommon
{
    /// <summary>
    /// Compiles a C# script into a DLL with dotnet-script, which needs to be installed
    /// at least into the context of the provided working directory before using this
    /// function. The resulting binary will have a GUID as a file name.
    /// </summary>
    /// <param name="scriptPath">Path to a *.csx file</param>
    /// <param name="outputDirIn">
    /// The parent directory in which the directory of published binaries will be put
    /// </param>
    /// <param name="workingDir">
    /// The working directory in which dotnet-script is run.
    /// </param>
    /// <returns>The path of the newly created DLL</returns>
    internal static AbsolutePath CompileScript(AbsolutePath scriptPath, AbsolutePath outputDirIn, AbsolutePath workingDir)
    {
        var dllName = Guid.NewGuid().ToString("N");
        var outputDir = outputDirIn / dllName;
        var dllPath = outputDir / (dllName + ".dll");
        outputDir.CreateOrCleanDirectory();
        DotNetTasks.DotNet(
            $"script publish \"{scriptPath}\" --dll -o {outputDir} -n {dllName}",
            workingDirectory: workingDir
        );
        return dllPath;
    }

    internal static AbsolutePath CompileProject(AbsolutePath projectPath, AbsolutePath outputDirIn)
    {
        var dllName = projectPath.NameWithoutExtension;
        var outputDir = outputDirIn / dllName;
        var dllPath = outputDir / (dllName + ".dll");
        outputDir.CreateOrCleanDirectory();

        DotNetTasks.DotNetBuild(_ => _
            .SetNoLogo(true)
            .SetProjectFile(projectPath)
            .SetOutputDirectory(outputDir)
            .SetConfiguration("Debug")
            .SetProcessWorkingDirectory(projectPath.Parent)
        );
        return dllPath;
    }

    /// <summary>
    /// Get the build interfaces of an input assembly inheriting Nuke.Common.INukeBuild
    /// </summary>
    internal static IEnumerable<Importable> GetBuildInterfaces(this Assembly assembly, AbsolutePath sourcePath, bool importViaSource = false) =>
        assembly.GetTypes()
            .Where(t => t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i => i.FullName == "Nuke.Common.INukeBuild"))
            .Select(t => new Importable(t, sourcePath, importViaSource));
}