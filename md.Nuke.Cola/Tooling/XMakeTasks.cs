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

/// <summary>
/// XMake is a versatile build tool for many languages https://xmake.io/#/?id=supported-languages
/// scriptable in Lua
/// </summary>
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

    /// <summary>
    /// Get XMake or an error if setup has failed.
    /// </summary>
    public static ValueOrError<Tool> EnsureXMake => ToolCola.Use("xmake", Setup);

    /// <summary>
    /// Get XMake. It throws an exception if setup has failed.
    /// </summary>
    public static Tool XMake => EnsureXMake.Get();
}