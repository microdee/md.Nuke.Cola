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
        Console.WriteLine("Initializing C# Script build plugin support");
        if (!(context.Temporary / ".config" / "dotnet-tools.json").FileExists())
        {
            Console.WriteLine("Setting up local dotnet tool manifest");
            DotNetTasks.DotNet("new tool-manifest", context.Temporary);
        }
        Console.WriteLine("Installing dotnet-script locally");
        DotNetTasks.DotNetToolInstall(_ => _
            .SetPackageName("dotnet-script")
            .SetGlobal(false)
            .SetProcessWorkingDirectory(context.Temporary)
        );
        
        var compiledRoot = context.Temporary / "CSharpScriptOutput";
        compiledRoot.CreateOrCleanDirectory();
    }
}