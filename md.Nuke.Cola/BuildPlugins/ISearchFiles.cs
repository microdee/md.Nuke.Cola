using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EverythingSearchClient;
using Nuke.Common;
using Nuke.Common.IO;

#pragma warning disable CA1416 // Validate platform compatibility

namespace Nuke.Cola.BuildPlugins;

/// <summary>
/// Simple interface for swapping file search engines
/// </summary>
public interface ISearchFiles
{
    IEnumerable<AbsolutePath> Glob(AbsolutePath root, string pattern);
    int Priority { get; }
}

public static class SearchFiles
{
    private static ISearchFiles? _current;

    public static ISearchFiles Get()
    {
        if (_current != null) return _current!;
        if (EnvironmentInfo.IsWin && SearchClient.IsEverythingAvailable())
        {
            _current = new EverythingGlobbing();
        }
        else
        {
            _current = new NukeGlobbing();
        }
        return _current;
    }
}

public class NukeGlobbing : ISearchFiles
{
    public IEnumerable<AbsolutePath> Glob(AbsolutePath root, string pattern) => root.GlobFiles(pattern);
    public int Priority => int.MaxValue;
}