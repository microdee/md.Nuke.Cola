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
        context.Root.GlobFiles("**/*.nuke.csx")
            .Select(f => new CSharpScriptPlugin
            {
                SourcePath = f
            });

    public void InitializeEngine(BuildContext context)
    {
        "Initializing C# Script build plugin support".Log();
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
    }
}