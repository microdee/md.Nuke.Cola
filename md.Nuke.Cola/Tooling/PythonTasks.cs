using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.Tooling;

namespace Nuke.Cola.Tooling;

public class PythonTasks
{
    public static ValueOrError<Tool> EnsurePython => ToolCola.Use("py");
    public static Tool Python => EnsurePython.Get();

    public static ValueOrError<Tool> EnsurePip => ToolCola.Use("pip");
    public static Tool Pip => EnsurePip.Get();
}