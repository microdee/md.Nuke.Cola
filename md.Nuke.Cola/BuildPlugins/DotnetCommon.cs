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
using Nuke.Common.Utilities.Collections;
using Standart.Hash.xxHash;

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
        ulong hash = xxHash64.ComputeHash(File.ReadAllText(scriptPath));
        var dllName = scriptPath.NameWithoutExtension + "_" + hash.ToString();
        var outputDir = outputDirIn / dllName;
        var dllPath = outputDir / (dllName + ".dll");

        if (dllPath.FileExists())
        {
            return dllPath;
        }

        Console.WriteLine($"Compiling script {scriptPath}");

        outputDir.CreateOrCleanDirectory();
        DotNetTasks.DotNet(
            $"script publish \"{scriptPath}\" --dll -o {outputDir} -n {dllName}",
            workingDirectory: workingDir
        );

        outputDirIn
            .GlobDirectories($"{scriptPath.NameWithoutExtension}_*")
            .Where(p => !p.Name.Contains(hash.ToString()))
            .ForEach(p => p.DeleteDirectory());

        return dllPath;
    }

    internal static AbsolutePath CompileProject(AbsolutePath projectPath, AbsolutePath outputDirIn)
    {
        ulong projectHash = xxHash64.ComputeHash(File.ReadAllText(projectPath));
        ulong hash = projectPath.Parent.GlobFiles("**/*.cs")
            .Aggregate(projectHash, (h, p) =>
                h ^ xxHash64.ComputeHash(File.ReadAllText(p))
            );

        var dllName = projectPath.NameWithoutExtension;
        var outputDir = outputDirIn / $"{dllName}_{hash}";
        var dllPath = outputDir / (dllName + ".dll");

        if (outputDir.DirectoryExists() && dllPath.FileExists())
        {
            return dllPath;
        }

        Console.WriteLine($"Compiling project {projectPath}");

        outputDir.CreateOrCleanDirectory();

        DotNetTasks.DotNetBuild(_ => _
            .SetNoLogo(true)
            .SetProjectFile(projectPath)
            .SetOutputDirectory(outputDir)
            .SetConfiguration("Debug")
            .SetProcessWorkingDirectory(projectPath.Parent)
        );
        
        outputDirIn
            .GlobDirectories($"{dllName}_*")
            .Where(p => !p.Name.Contains(hash.ToString()))
            .ForEach(p => p.DeleteDirectory());

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