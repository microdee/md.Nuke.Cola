using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using EverythingSearchClient;
using Nuke.Common.IO;

namespace Nuke.Cola.Search;

[SupportedOSPlatform("windows")]
public class EverythingGlobbing : ISearchFileSystem
{
    readonly SearchClient _everything = new();

    public int Priority => 0;

    private Result Glob(AbsolutePath root, string pattern)
    {
        var absolutePattern = (root / pattern).ToString();

        // Nuke globbing **/ includes starting directory of recursion however Everything only starts
        // recursion with subdirectories. In order to emulate Nuke globbing we repeat the pattern
        // **/ excluded
        var processedPattern = absolutePattern.Contains("**/") || absolutePattern.Contains("**\\")
            ? absolutePattern + "|" + absolutePattern.Replace("**/", "").Replace("**\\", "")
            : absolutePattern;

        return _everything.Search(
            processedPattern,
            SearchClient.SearchFlags.MatchPath | SearchClient.SearchFlags.MatchWholeWord,
            SearchClient.BehaviorWhenBusy.WaitOrContinue
        );
    }

    public IEnumerable<AbsolutePath> GlobFiles(AbsolutePath root, string pattern)
        => Glob(root, pattern).Items
            .Where(i => i.Flags == Result.ItemFlags.None)
            .Select(i => ((AbsolutePath) i.Path) / i.Name);

    public IEnumerable<AbsolutePath> GlobDirectories(AbsolutePath root, string pattern)
        => Glob(root, pattern).Items
            .Where(i => i.Flags == Result.ItemFlags.Folder)
            .Select(i => ((AbsolutePath) i.Path) / i.Name);
}