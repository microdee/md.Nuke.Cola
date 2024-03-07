using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

namespace Nuke.Cola.BuildPlugins;

/// <summary>
/// Gather build plugins defined as single file C# scripts following the
/// file name format `*.nuke.csx`.
/// </summary>
public class CSharpScriptPluginProvider : IProvidePlugins
{
    public IEnumerable<IHavePlugin> GatherPlugins(BuildContext context) =>
        SearchFiles.Get().Glob(context.Root, "**/*.nuke.csx")
            .Select(f => new CSharpScriptPlugin
            {
                SourcePath = f
            });

    public void InitializeEngine(BuildContext context)
    {
        "Initializing C# Script build plugin support".Log();
        var lockFile = context.Temporary / ".config" / "dotnet-script.installed";
        if (lockFile.FileExists())
        {
            return;
        }
        if (!(context.Temporary / ".config" / "dotnet-tools.json").FileExists())
        {
            "Setting up local dotnet tool manifest".Log();
            DotNetTasks.DotNet("new tool-manifest", context.Temporary);
        }
        "Installing dotnet-script locally".Log();
        DotNetTasks.DotNetToolInstall(_ => _
            .SetPackageName("dotnet-script")
            .SetGlobal(false)
            .SetProcessWorkingDirectory(context.Temporary)
        );
        
        "Initializing CSX support for VSCode".Log();
        var templateScript = context.Root / "temp.csx";
        DotNetTasks.DotNet(
            $"script init {templateScript.Name} --workingdirector \"{context.Root}\"",
            context.Root
        );
        templateScript.DeleteFile();
        
        File.WriteAllText(lockFile, "installed");
    }
}