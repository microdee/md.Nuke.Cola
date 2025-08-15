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
/// Gather build plugins defined as single file C# scripts following the
/// file name format `*.nuke.csx`.
/// </summary>
public class CSharpScriptPluginProvider : IProvidePlugins
{
    public IEnumerable<IHavePlugin> GatherPlugins(BuildContext context)
        => context.Root.SearchFiles("**/*.nuke.csx")
            .Select(f => new CSharpScriptPlugin
            {
                SourcePath = f
            });

    public void InitializeEngine(BuildContext context) {}
}