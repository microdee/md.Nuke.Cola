using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace Nuke.Cola.BuildPlugins;

public record BuildContext(AbsolutePath Temporary, AbsolutePath Root);