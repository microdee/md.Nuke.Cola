using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Cola.Search;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

namespace Nuke.Cola.BuildPlugins;

/// <summary>
/// Gather build plugins defined as a .NET project following the
/// file name format `*.Nuke.csproj`. Currently only C# is supported
/// as only C# can declare default interface member implementations.
/// </summary>
public class DotnetProjectPluginProvider : IProvidePlugins
{
    public IEnumerable<IHavePlugin> GatherPlugins(BuildContext context)
        => context.Root.SearchFiles("**/*.Nuke.csproj")
            .Select(f => new DotnetProjectPlugin
            {
                SourcePath = f
            });

    public void InitializeEngine(BuildContext context) { }
}