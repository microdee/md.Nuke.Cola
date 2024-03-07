using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using EverythingSearchClient;
using Nuke.Common.IO;

namespace Nuke.Cola.BuildPlugins;

[SupportedOSPlatform("windows")]
public class EverythingGlobbing : ISearchFiles
{
    SearchClient _everything = new();

    public int Priority => 0;

    public IEnumerable<AbsolutePath> Glob(AbsolutePath root, string pattern)
    {
        var absolutePattern = (root / pattern).ToString();
        var result = _everything.Search(absolutePattern, SearchClient.SearchFlags.MatchPath, SearchClient.BehaviorWhenBusy.WaitOrContinue);
        return result.Items
            .Where(i => i.Flags == Result.ItemFlags.None)
            .Select(i => ((AbsolutePath) i.Path) / i.Name);
    }
}