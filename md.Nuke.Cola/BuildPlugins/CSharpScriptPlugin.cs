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
            DotnetScript.CompileDll(
                SourcePath,
                compiledRoot,
                context.Temporary
            )
        );

        _buildInterfaces = assembly.GetTypes()
            .Where(t => t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i => i.FullName == "Nuke.Common.INukeBuild"))
            .Select(t => new Importable(t, SourcePath, true))
            .ToList();
    }
}