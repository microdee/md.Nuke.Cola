using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GlobExpressions;
using Nuke.Cola;
using Nuke.Common.IO;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Serilog;
using YamlDotNet.Serialization;

namespace Nuke.Cola.FolderComposition;

/// <summary>
/// A union provided for denoting wether we want to link/copy a file or a directory.
/// It is undefined behavior when both File and Directory is set to non-null value.
/// 
/// Addtitionally specify some options about the method of exporting given item.
/// </summary>
public class FileOrDirectory : ICloneable<FileOrDirectory>
{
    /// <summary>
    /// Export a single or a glob of files handled individually. Either File or Directory (dir)
    /// must be specified.
    /// </summary>
    [YamlMember]
    public string? File;

    /// <summary>
    /// Export one or a glob of directories handled recursively. Files inside target directories are
    /// not considered. Either File or Directory (dir) must be specified.
    /// </summary>
    [YamlMember(Alias = "dir")]
    public string? Directory;

    /// <summary>
    /// Exclude iterms from this particular set of files or directories if they match any of these patterns
    /// </summary>
    [YamlMember(Alias = "not")]
    public List<string> Not = [];

    /// <summary>
    /// Override the destination relative path of exported item.
    /// 
    /// Use `$N` syntax (where N is 1..(number of * or **)) to reuse those captured segments of the
    /// globbing.
    /// 
    /// Use `$#` syntax to get the 0 based ID of globbed item.
    /// </summary>
    [YamlMember]
    public string? As;

    /// <summary>
    /// When working with a file, process its contents for replacing specified suffixes
    /// </summary>
    [YamlMember(Alias = "procContent")]
    public bool ProcessContent = false;

    /// <summary>
    /// Only used by "use", if a subfolder uses a different file for export manifest, specify that
    /// via this glob. Default is "export.y*ml" or whatever else has been specified for this import
    /// session.
    /// </summary>
    [YamlMember(Alias = "manifestFilePattern")]
    public string? ManifestFilePattern;

    internal AbsolutePath? GetDestination(AbsolutePath srcRoot, AbsolutePath dstRoot, AbsolutePath currentPath, int itemId, IEnumerable<string> exclude)
    {
        var glob = (File ?? Directory)!;
        var relativePath = srcRoot.GetRelativePathTo(currentPath);

        bool Ignore(string glob)
        {
            var regex = glob.GlobToRegex();
            return Regex.IsMatch(relativePath!.ToString(), regex, RegexOptions.IgnoreCase);
        }

        if (exclude.Any(Ignore))
            return null;
        
        if (As == null)
            return dstRoot / relativePath;

        var asExpr = As.Replace("$#", itemId.ToString());

        if (glob.Contains('*') && asExpr.Contains('$'))
        {
            var asResult = asExpr;
            var relPath = relativePath.ToString().Replace("\\", "/");
            var regex = glob.GlobToRegex();
            var match = Regex.Match(relPath, regex);
            for (int i = 1; i < match.Groups.Count; i++)
            {
                asResult = asResult.Replace($"${i}", match.Groups[i]?.Value);
            }

            return dstRoot / asResult.Replace("//", "/");
        }
        else return dstRoot / asExpr;
    }

    public FileOrDirectory Clone()
    {
        return new()
        {
            File = File,
            Directory = Directory,
            Not = [.. Not],
            As = As,
            ProcessContent = ProcessContent,
            ManifestFilePattern = ManifestFilePattern
        };
    }

    object ICloneable.Clone()
    {
        return Clone();
    }
}

/// <summary>
/// Controls how a folder should be exported for composition.
/// It is meant to be used with export.yml YAML files (or export manifest files).
/// </summary>
public class ExportManifest : ICloneable<ExportManifest>
{
    /// <summary>
    /// A list of items which will be symlinked. Content processing will obviously not happen in this case.
    /// </summary>
    [YamlMember]
    public List<FileOrDirectory> Link = [];
    
    /// <summary>
    /// A list of items which will be copied. Content processing can happen in this case if item is
    /// flagged to do so.
    /// </summary>
    [YamlMember]
    public List<FileOrDirectory> Copy = [];
    
    /// <summary>
    /// A list of folders which should contain an export manifest, or files which points to export
    /// manifests. If a given folder doesn't contain an export.yml or the given file is not an
    /// export.yml then those will be ignored with noop.
    /// ProcessContent is ignored in this list as that's controlled by the imported manifests.
    /// Globbing is also supported, simply writing `**` in Directory (dir) will import all subfolders
    /// containing an `export.yml` manifest file.
    /// </summary>
    [YamlMember]
    public List<FileOrDirectory> Use = [];
    
    /// <summary>
    /// Ignore files or directories matching any of these patterns from this entire export
    /// </summary>
    [YamlMember]
    public List<string> Not = [];

    /// <summary>
    /// Merge one manifest with another. This will simply append items to each lists.
    /// </summary>
    public void Add(ExportManifest? other)
    {
        if (other == null) return;
        Link.AddRange(other.Link);
        Copy.AddRange(other.Copy);
        Use.AddRange(other.Use);
        Not.AddRange(other.Not);
    }

    public ExportManifest Clone()
    {
        return new()
        {
            Link = [.. Link.Select(s => s.Clone())],
            Copy = [.. Copy.Select(s => s.Clone())],
            Use = [.. Use.Select(s => s.Clone())],
            Not = [.. Not],
        };
    }

    object ICloneable.Clone()
    {
        return Clone();
    }
}

public static class ExportManifestExtensions
{
    /// <summary>
    /// Combine input export manifests together into given one. If given is null, the first one will
    /// be cloned from the others-
    /// </summary>
    /// <returns>
    /// Return `self`, or the first valid export manifest in `others`. For this reason the return
    /// object might not be the same as `self` if `self` was originally null.
    /// </returns>
    public static ExportManifest? Combine(this ExportManifest? self, IEnumerable<ExportManifest>? others)
    {
        if (others == null || others.IsEmpty()) return self;
        if (self == null)
        {
            self = others.First().Clone();
            others = others.Skip(1);
        }
        foreach (var other in others)
        {
            self!.Add(other);
        }
        return self;
    }
}