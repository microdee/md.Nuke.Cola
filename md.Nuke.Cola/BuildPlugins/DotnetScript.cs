using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

namespace Nuke.Cola.BuildPlugins;

public static class DotnetScript
{
    public static AbsolutePath CompileDll(AbsolutePath scriptPath, AbsolutePath outputDirIn, AbsolutePath workingDir)
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
}