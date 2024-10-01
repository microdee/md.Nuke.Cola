using EverythingSearchClient;
using Nuke.Common;
using Nuke.Common.IO;

#pragma warning disable CA1416 // Validate platform compatibility

namespace Nuke.Cola.Search;

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
        if (EnvironmentInfo.IsWin && SearchClient.IsEverythingAvailable() && false)
        {
            _current = new EverythingGlobbing();
        }
        else
        {
            _current = new NukeGlobbing();
        }
        return _current;
    }

    /// <summary>
    /// Very similar to GlobFiles but uses different engines for better performance depenging on
    /// user's setup and platform.
    /// </summary>
    /// <remarks>
    /// As of time of writing the following engines available:
    /// 
    /// * Nuke built in Globbing
    /// * Voidtools Everything if installed on the system
    /// </remarks>
    public static IEnumerable<AbsolutePath> SearchFiles(this AbsolutePath root, string pattern)
        => GetGlobbing().GlobFiles(root, pattern);


    /// <summary>
    /// Very similar to GlobDirectories but uses different engines for better performance depenging on
    /// user's setup and platform.
    /// </summary>
    /// <remarks>
    /// As of time of writing the following engines available:
    /// 
    /// * Nuke built in Globbing
    /// * Voidtools Everything if installed on the system
    /// </remarks>
    public static IEnumerable<AbsolutePath> SearchDirectories(this AbsolutePath root, string pattern)
        => GetGlobbing().GlobDirectories(root, pattern);
}

public class NukeGlobbing : ISearchFileSystem
{
    public IEnumerable<AbsolutePath> GlobFiles(AbsolutePath root, string pattern)
        => root.GlobFiles(pattern);
    public IEnumerable<AbsolutePath> GlobDirectories(AbsolutePath root, string pattern)
        => root.GlobDirectories(pattern);
        
    public int Priority => int.MaxValue;
}