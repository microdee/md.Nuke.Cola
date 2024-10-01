using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Cola;

public static class NukeBuildExtensions
{
    public static AbsolutePath ScriptFolder(this INukeBuild self, [CallerFilePath] string? script = null)
        => ((AbsolutePath) script!).Parent;
}