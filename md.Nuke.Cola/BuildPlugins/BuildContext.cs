using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace Nuke.Cola.BuildPlugins;

/// <summary>
/// Local paths for plugin discovery and compilation.
/// </summary>
public record BuildContext(AbsolutePath Temporary, AbsolutePath Root);