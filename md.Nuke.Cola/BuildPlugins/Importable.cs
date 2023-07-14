using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace Nuke.Cola.BuildPlugins;
public record Importable(Type Interface, AbsolutePath Source, bool ImportViaSource = false)
{
    public override string ToString() => ImportViaSource ? Source.ToString() : Interface.Assembly.Location;
}