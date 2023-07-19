using System.Reflection;
using Nuke.Common.IO;

namespace Nuke.Cola.BuildPlugins;

/// <summary>
/// Encapsulates a .NET project (*.*proj) which contains build interfaces.
/// </summary>
public class DotnetProjectPlugin : IHavePlugin
{
    private List<Importable> _buildInterfaces = new();
    public IEnumerable<Importable> BuildInterfaces => _buildInterfaces;

    public AbsolutePath SourcePath { init; get; } = (AbsolutePath) "/";

    public void Compile(BuildContext context)
    {
        var compiledRoot = context.Temporary / "DotnetProjectOutput";

        var assembly = Assembly.LoadFrom(
            DotnetCommon.CompileProject(SourcePath, compiledRoot)
        );

        _buildInterfaces = assembly.GetBuildInterfaces(SourcePath, false).ToList();
    }
}