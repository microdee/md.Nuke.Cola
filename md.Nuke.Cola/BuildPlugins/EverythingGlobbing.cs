using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using EverythingSearchClient;
using Nuke.Common.IO;

namespace Nuke.Cola.BuildPlugins;

[SupportedOSPlatform("windows")]
public class EverythingGlobbing : ISearchFileSystem
{
    readonly SearchClient _everything = new();

    public int Priority => 0;

    private Result Glob(AbsolutePath root, string pattern)
    {
        var absolutePattern = (root / pattern).ToString();
        return _everything.Search(absolutePattern, SearchClient.SearchFlags.MatchPath, SearchClient.BehaviorWhenBusy.WaitOrContinue);
    }

    public IEnumerable<AbsolutePath> GlobFiles(AbsolutePath root, string pattern) =>
        Glob(root, pattern).Items
            .Where(i => i.Flags == Result.ItemFlags.None)
            .Select(i => ((AbsolutePath) i.Path) / i.Name);

    public IEnumerable<AbsolutePath> GlobDirectories(AbsolutePath root, string pattern) =>
        Glob(root, pattern).Items
            .Where(i => i.Flags == Result.ItemFlags.Folder)
            .Select(i => ((AbsolutePath) i.Path) / i.Name);
}