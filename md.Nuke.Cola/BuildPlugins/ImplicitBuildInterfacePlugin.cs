using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace Nuke.Cola.BuildPlugins;

public class ImplicitBuildInterfacePlugin : IHavePlugin
{
    public List<Importable> Interfaces { init; get; } = new();
    public IEnumerable<Importable> BuildInterfaces => Interfaces;

    public AbsolutePath SourcePath => (AbsolutePath) $"\\\\{nameof(ImplicitBuildInterfacePlugin)}";

    public void Compile(BuildContext context) {}
}