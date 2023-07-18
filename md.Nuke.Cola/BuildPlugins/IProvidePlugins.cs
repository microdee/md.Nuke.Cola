using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

namespace Nuke.Cola.BuildPlugins;

/// <summary>
/// Implementations of this interface must represent a plugin system which might
/// have a distinct logic and rules for discovering individual plugins and
/// instantiating them.
/// </summary>
public interface IProvidePlugins
{
    /// <summary>
    /// One time initialization of the provider.
    /// </summary>
    void InitializeEngine(BuildContext context);

    /// <summary>
    /// Gathering plugins from the provided context
    /// </summary>
    /// <param name="context"></param>
    /// <returns>A collection of individual plugins</returns>
    IEnumerable<IHavePlugin> GatherPlugins(BuildContext context);
}