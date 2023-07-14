using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

namespace Nuke.Cola.BuildPlugins;

public interface IProvidePlugins
{
    void InitializeEngine(BuildContext context);
    IEnumerable<IHavePlugin> GatherPlugins(BuildContext context);
}