using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nuke.Cola;
using Nuke.Common.IO;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Serilog;
using YamlDotNet.Serialization;

/// <summary>
/// A union provided for denoting wether we want to link/copy a file or a directory.
/// It is undefined behavior when both File and Directory is set to non-null value.
/// 
/// Addtitionally specify some options about the method of exporting given item.
/// </summary>
public class FileOrDirectory
{
    /// <summary>
    /// Export a single or a glob of files handled individually. Either File or Directory (dir)
    /// must be specified.
    /// </summary>
    [YamlMember]
    public string? File;

    /// <summary>
    /// When working with a file, process its contents for replacing specified suffixes
    /// </summary>
    [YamlMember(Alias = "procContent")]
    public bool ProcessContent = false;

    /// <summary>
    /// Export one or a glob of directories handled recursively. Files inside target directories are
    /// not considered. Either File or Directory (dir) must be specified.
    /// </summary>
    [YamlMember(Alias = "dir")]
    public string? Directory;

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

    internal AbsolutePath GetDestination(AbsolutePath srcRoot, AbsolutePath dstRoot, AbsolutePath currentPath, int itemId)
    {
        var glob = (File ?? Directory)!;
        var relativePath = srcRoot.GetRelativePathTo(currentPath);
        
        if (As == null)
            return dstRoot / relativePath;

        var asExpr = As.Replace("$#", itemId.ToString());

        if (glob.Contains('*') && asExpr.Contains('$'))
        {
            var asResult = asExpr;
            var relPath = relativePath.ToString().Replace("\\", "/");
            var regex = glob.GlobToRegex();
            var match = Regex.Match(relPath, regex);
            for (int i = 0; i < match.Groups.Count; i++)
            {
                asResult = asResult.Replace($"${i}", match.Groups[i]?.Value);
            }

            return dstRoot / asResult.Replace("//", "/");
        }
        else return dstRoot / asExpr;
    }
}

/// <summary>
/// Controls how a folder should be exported for composition.
/// It is meant to be used with export.yml YAML files.
/// </summary>
public class ExportManifest
{
    /// <summary>
    /// A list of items which will be symlinked. Content processing will obviously not happen in this case.
    /// </summary>
    [YamlMember]
    public List<FileOrDirectory> Link = new();
    
    /// <summary>
    /// A list of items which will be copied. Content processing can happen in this case if item is
    /// flagged to do so.
    /// </summary>
    [YamlMember]
    public List<FileOrDirectory> Copy = new();
}