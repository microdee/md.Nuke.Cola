using System.Reflection;
using Nuke.Common.IO;

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