using System.Dynamic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuke.Common;
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
/// Mirror of package-repo object which may be returned by xrepo fetch.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public record class XRepoPackageRepo(

    [JsonProperty(PropertyName = "commit")]
    string Commit,

    [JsonProperty(PropertyName = "url")]
    string Url,

    [JsonProperty(PropertyName = "branch")]
    string Branch,

    [JsonProperty(PropertyName = "name")]
    string Name
);

/// <summary>
/// Mirror of package-artifact object which may be returned by xrepo fetch.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public record class XRepoPackageArtifact(

    [JsonProperty(PropertyName = "installdir")]
    string InstallDir
);

/// <summary>
/// Mirror of package object which is returned by xrepo fetch.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public record class XRepoPackage(

    [JsonProperty(PropertyName = "version")]
    string Version,

    [JsonProperty(PropertyName = "license")]
    string? License = null,

    [JsonProperty(PropertyName = "links")]
    [JsonConverter(typeof(OneOrManyConverter<string>))]
    List<string>? Links = null,

    [JsonProperty(PropertyName = "syslinks")]
    [JsonConverter(typeof(OneOrManyConverter<string>))]
    List<string>? SysLinks = null,

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
    [JsonConverter(typeof(OneOrManyConverter<string>))]
    List<string>? Defines = null,

    // These only appear in system program dependencies
    [JsonProperty(PropertyName = "name")]
    string? Name = null,

    [JsonProperty(PropertyName = "program")]
    AbsolutePath? Program = null,

    // These only appear in auto-environment program dependencies
    [JsonProperty(PropertyName = "arch")]
    string? Arch = null,

    [JsonProperty(PropertyName = "configs")]
    Dictionary<string, string>? Configs = null,

    [JsonProperty(PropertyName = "description")]
    string? Description = null,

    [JsonProperty(PropertyName = "plat")]
    string? Plat = null,

    [JsonProperty(PropertyName = "envs")]
    Dictionary<string, object>? Envs = null,

    [JsonProperty(PropertyName = "repo")]
    XRepoPackageRepo? Repo = null,
    
    // TODO deps and librarydeps

    [JsonProperty(PropertyName = "pathenvs")]
    string[]? PathEnvs = null,

    [JsonProperty(PropertyName = "kind")]
    string? Kind = null,

    [JsonProperty(PropertyName = "artifacts")]
    XRepoPackageArtifact? Artifacts = null,

    [JsonProperty(PropertyName = "mode")]
    string? Mode = null
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
    
    public bool IsSystemProgram => Program != null;
    public bool IsEnvironmentProgram => !IsSystemProgram && (Kind?.EqualsOrdinalIgnoreCase("binary") ?? false);
    public bool IsProgram => IsSystemProgram || IsEnvironmentProgram;
    public bool IsLibrary => !IsProgram;

    /// <summary>
    /// Is this package a header only library
    /// </summary>
    public bool IsHeaderOnly => (!IncludeDirs.IsNullOrEmpty() || !SysIncludeDirs.IsNullOrEmpty())
        && IsLibrary
        && LinkDirs.IsNullOrEmpty()
        && LibFiles.IsNullOrEmpty()
    ;

    public JObject? GetManifest()
    {
        var path = GetPath();
        if (path == null) return null;
        var manifestJsonPath = path.PackageFolder / "manifest.json";
        manifestJsonPath.ExistingFile()?.Delete();
        var tempLuaPath = path.PackageFolder / "convert_manifest.lua";
        tempLuaPath.WriteAllText(
            """
            import("core.base.json")
            local manifest = io.load("manifest.txt")
            json.savefile("manifest.json", manifest)
            """
        );
        XMakeTasks.XMake("lua ./convert_manifest.lua", workingDirectory: path.PackageFolder);
        Assert.FileExists(manifestJsonPath, "XMake didn't generate a json version of the manifest file. It may have logged why.");
        return manifestJsonPath.ReadJson();
    }
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
        => packages.Where(p => p.IsLibrary)
            .FirstOrDefault(p => p.InferredName.EqualsOrdinalIgnoreCase(name));

    /// <summary>
    /// Get a specific program from a collection of XRepo packages
    /// </summary>
    /// <param name="packages"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static XRepoPackage? GetProgram(this IEnumerable<XRepoPackage> packages, string name)
        => packages.Where(p => p.IsProgram)
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
