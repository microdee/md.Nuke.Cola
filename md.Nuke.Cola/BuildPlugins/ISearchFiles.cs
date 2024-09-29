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
public interface ISearchFileSystem
{
    IEnumerable<AbsolutePath> GlobFiles(AbsolutePath root, string pattern);
    IEnumerable<AbsolutePath> GlobDirectories(AbsolutePath root, string pattern);
    int Priority { get; }
}

public static class SearchFileSystem
{
    private static ISearchFileSystem? _current;

    private static ISearchFileSystem GetGlobbing()
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

    public static IEnumerable<AbsolutePath> SearchFiles(this AbsolutePath root, string pattern) =>
        GetGlobbing().GlobFiles(root, pattern);

    public static IEnumerable<AbsolutePath> SearchDirectories(this AbsolutePath root, string pattern) =>
        GetGlobbing().GlobDirectories(root, pattern);
}

public class NukeGlobbing : ISearchFileSystem
{
    public IEnumerable<AbsolutePath> GlobFiles(AbsolutePath root, string pattern) => root.GlobFiles(pattern);
    public IEnumerable<AbsolutePath> GlobDirectories(AbsolutePath root, string pattern) => root.GlobDirectories(pattern);
    public int Priority => int.MaxValue;
}