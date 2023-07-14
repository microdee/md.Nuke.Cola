using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace Nuke.Cola.BuildPlugins;

public interface IHavePlugin
{
    void Compile(BuildContext context);
    IEnumerable<Importable> BuildInterfaces { get; }
    AbsolutePath SourcePath { get; }
}