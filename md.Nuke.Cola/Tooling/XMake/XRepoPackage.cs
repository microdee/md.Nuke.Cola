using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Cola.Tooling.XMake;

public partial record class XRepoPackagePath(
    AbsolutePath ParentFolder,
    string Name,
    string Version,
    string Hash
) {

    [GeneratedRegex(
        """
        ^
        (?<PARENT>.*?[\/\\]packages[\/\\]\w)[\/\\]
        (?<NAME>.+?)[\/\\]
        (?<VERSION>.+?)[\/\\]
        (?<HASH>.+?)(?:[\/\\]|$)
        """,
        RegexOptions.CultureInvariant
        | RegexOptions.IgnoreCase
        | RegexOptions.IgnorePatternWhitespace
    )]
    private static partial Regex PathParserPattern();

    /// <summary>
    /// Parse an input path to XRepoPackagePath
    /// </summary>
    /// <param name="from"></param>
    /// <returns>An XRepoPackagePath if that could be inferred</returns>
    public static XRepoPackagePath? Make(AbsolutePath from)
    {
        var parsed = from.ToString().Parse(PathParserPattern(), forceNullOnWhitespce: true);
        var parentFolder = parsed("PARENT");
        var name = parsed("NAME");
        var version = parsed("VERSION");
        var hash = parsed("HASH");
        if (parentFolder == null || name == null || version == null || hash == null)
        {
            return null;
        }

        return new(parentFolder, name, version, hash);
    }

    /// <summary>
    /// Get the root folder of all packages.
    /// </summary>
    public AbsolutePath PackagesRoot => ParentFolder.Parent!;

    /// <summary>
    /// Get the folder of this package
    /// </summary>
    public AbsolutePath PackageFolder => ParentFolder / Name / Version / Hash;
}

/// <summary>
/// Mirror of package object which is returned by xrepo fetch.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public record class XRepoPackage(

    [JsonProperty(PropertyName = "version")]
    string Version,

    [JsonProperty(PropertyName = "license")]
    string? License = null,

    [JsonProperty(PropertyName = "name")]
    string? Name = null,

    [JsonProperty(PropertyName = "links")]
    [JsonConverter(typeof(OneOrManyConverter<string>))]
    List<string>? Links = null,

    [JsonProperty(PropertyName = "syslinks")]
    [JsonConverter(typeof(OneOrManyConverter<string>))]
    List<string>? SysLinks = null,

    [JsonProperty(PropertyName = "program")]
    AbsolutePath? Program = null,

    [JsonProperty(PropertyName = "static")]
    bool? Static = false,

    [JsonProperty(PropertyName = "shared")]
    bool? Shared = false,

    [JsonProperty(PropertyName = "linkdirs")]
    AbsolutePath[]? LinkDirs = null,

    [JsonProperty(PropertyName = "libfiles")]
    AbsolutePath[]? LibFiles = null,

    [JsonProperty(PropertyName = "includedirs")]
    AbsolutePath[]? IncludeDirs = null,

    [JsonProperty(PropertyName = "sysincludedirs")]
    AbsolutePath[]? SysIncludeDirs = null,

    [JsonProperty(PropertyName = "cxxflags")]
    string? CxxFlags = null,

    [JsonProperty(PropertyName = "defines")]
    string? Defines = null
)
{
    /// <summary>
    /// Get the parsed path of this package. This is only available for libraries
    /// </summary>
    /// <returns></returns>
    public XRepoPackagePath? GetPath()
    {
        foreach (var includeDir in IncludeDirs ?? [])
        {
            var result = includeDir.InferXRepoPackage();
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Since XMake doesn't append the library name directly to package information using `fetch` we will need to
    /// do some guess-work
    /// </summary>
    public string? InferredName => Name ?? GetPath()?.Name;

    /// <summary>
    /// Is this package a header only library
    /// </summary>
    public bool IsHeaderOnly => (!IncludeDirs.IsNullOrEmpty() || !SysIncludeDirs.IsNullOrEmpty())
        && Program == null
        && LinkDirs.IsNullOrEmpty()
        && LibFiles.IsNullOrEmpty()
    ;
}

public static class XRepoPackageExtensions
{
    public static XRepoPackagePath? InferXRepoPackage(this AbsolutePath path) => XRepoPackagePath.Make(path);

    /// <summary>
    /// Get a specific library from a collection of XRepo packages
    /// </summary>
    /// <param name="packages"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static XRepoPackage? GetLibrary(this IEnumerable<XRepoPackage> packages, string name)
        => packages.Where(p => p.Program == null)
            .FirstOrDefault(p => p.InferredName.EqualsOrdinalIgnoreCase(name));

    /// <summary>
    /// Get a specific program from a collection of XRepo packages
    /// </summary>
    /// <param name="packages"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static XRepoPackage? GetProgram(this IEnumerable<XRepoPackage> packages, string name)
        => packages.Where(p => p.Program != null)
            .FirstOrDefault(p => p.InferredName.EqualsOrdinalIgnoreCase(name));

    /// <summary>
    /// Parse the Tool output of XRepoTasks.Info into structured data.
    /// </summary>
    public static List<XRepoPackage>? ParseXRepoFetch(this IEnumerable<Output> output)
    {
        foreach (var line in output)
        {
            if (line.Text.StartsWith("[{") && line.Text.EndsWith("]"))
            {
                return line.Text.GetJson<List<XRepoPackage>>();
            }
        }

        return null;
    }
}
