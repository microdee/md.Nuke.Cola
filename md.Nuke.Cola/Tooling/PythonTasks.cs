using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.Tooling;

namespace Nuke.Cola.Tooling;

public class PythonTasks
{
    public static ValueOrError<ToolEx> EnsurePython => ToolCola.Use("py");
    public static ToolEx Python => EnsurePython.Get();

    public static ValueOrError<ToolEx> EnsurePip => ToolCola.Use("pip");
    public static ToolEx Pip => EnsurePip.Get();
}
