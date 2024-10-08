using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace Nuke.Cola.BuildPlugins;

/// <summary>
/// A record for storing compiled build plugin interfaces
/// </summary>
/// <param name="Interface">.NET type of the build interface</param>
/// <param name="Source">Original source path of the plugin (can be either a script or a project file)</param>
/// <param name="ImportViaSource">
/// When true the intermediate script will use `#load "&lt;source&gt;"` directive instead of `#r "&lt;dll&gt;"`
/// when importing the build plugin. This was only intended to be used with C# script plugins.
/// </param>
public record Importable(Type Interface, AbsolutePath? Source = null, bool ImportViaSource = false)
{
    public override string ToString() => Source == null
        ? ""
        : ImportViaSource
            ? Source.ToString()
            : Interface.Assembly.Location;
}