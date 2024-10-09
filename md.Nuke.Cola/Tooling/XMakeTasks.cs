using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.PowerShell;
using static Nuke.Cola.Cola;

namespace Nuke.Cola.Tooling;

public static class XMakeTasks
{
    internal static void Setup()
    {
        if (EnvironmentInfo.Platform == PlatformFamily.Windows)
            PowerShellTasks.PowerShell(
                "-Command { iex (iwr 'https://xmake.io/psget.text').ToString() }",
                environmentVariables: MakeDictionary(("CI", "1"))
            );
        else
            ProcessTasks.StartShell("curl -fsSL https://xmake.io/shget.text | bash").AssertWaitForExit();
    }

    public static ValueOrError<Tool> EnsureXMake => ToolCola.GetPathTool("xmake", Setup);

    public static Tool XMake => EnsureXMake.Get();
}