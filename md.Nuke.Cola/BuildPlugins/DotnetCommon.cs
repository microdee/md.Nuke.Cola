using System.Reflection;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using Dotnet.Script.Core.Commands;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Environment;
using Standart.Hash.xxHash;
using Dotnet.Script.Core;
using Microsoft.CodeAnalysis;

namespace Nuke.Cola.BuildPlugins;

internal static class DotnetCommon
{
    private static LogFactory? DotnetScriptLogFactoryInstance = null;
    private static LogFactory DotnetScriptLogFactory => DotnetScriptLogFactoryInstance
        ??= new(type => (level, message, except) =>
        {
            if (level > LogLevel.Debug)
                message.Log();
}       );

    /// <summary>
    /// Compiles a C# script into a DLL with dotnet-script, which needs to be installed
    /// at least into the context of the provided working directory before using this
    /// function. The resulting binary will have a GUID as a file name.
    /// </summary>
    /// <param name="scriptPath">Path to a *.csx file</param>
    /// <param name="outputDirIn">
    /// The parent directory in which the directory of published binaries will be put
    /// </param>
    /// <returns>The path of the newly created DLL</returns>
    internal static AbsolutePath CompileScript(AbsolutePath scriptPath, AbsolutePath outputDirIn)
    {
        uint pathHash = xxHash32.ComputeHash(scriptPath);
        ulong hash = xxHash64.ComputeHash(scriptPath.ReadAllText());
        var dllName = hash.ToString();
        var outputDir = outputDirIn / (pathHash.ToString() + "_" + dllName);
        var dllPath = outputDir / (dllName + ".dll");

        if (dllPath.FileExists())
            return dllPath;

        $"Compiling script {scriptPath}".Log();

        outputDir.CreateOrCleanDirectory();
        new PublishCommand(ScriptConsole.Default, DotnetScriptLogFactory).Execute(new(
            new(scriptPath),
            outputDir,
            dllName,
            PublishType.Library,
            OptimizationLevel.Debug,
            [],
            ScriptEnvironment.Default.RuntimeIdentifier,
            false
        ));

        // Remove residue of previous build results
        outputDirIn
            .GlobDirectories($"{pathHash}_*")
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

        $"Compiling project {projectPath}".Log();

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
    internal static IEnumerable<Importable> GetBuildInterfaces(this Assembly assembly, AbsolutePath? sourcePath = null, bool importViaSource = false)
        => assembly.GetTypes()
            .Where(t => t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i => i.FullName == "Nuke.Common.INukeBuild"))
            .Select(t => new Importable(t, sourcePath, importViaSource));

    internal static void Log(this string text)
    {
        if (!Environment.CommandLine.Contains(" :complete"))
        {
            Console.WriteLine(text);
        }
    }
}