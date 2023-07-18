using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;

namespace Nuke.Cola.BuildPlugins;

/// <summary>
/// Encapsulates a C# script file (*.csx) which contains build interfaces.
/// </summary>
public class CSharpScriptPlugin : IHavePlugin
{
    private List<Importable> _buildInterfaces = new();
    public IEnumerable<Importable> BuildInterfaces => _buildInterfaces;

    public AbsolutePath SourcePath { init; get; } = (AbsolutePath) "/";

    public void Compile(BuildContext context)
    {
        Console.WriteLine($"Compiling build plugin script {SourcePath}");
        var compiledRoot = context.Temporary / "CSharpScriptOutput";

        var assembly = Assembly.LoadFrom(
            DotnetCommon.CompileScript(
                SourcePath,
                compiledRoot,
                context.Temporary
            )
        );

        _buildInterfaces = assembly.GetBuildInterfaces(SourcePath, true).ToList();
    }
}