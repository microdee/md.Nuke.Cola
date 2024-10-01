using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;
using YamlDotNet.Serialization;

/// <summary>
/// A union provided for denoting wether we want to link/copy a file or a directory.
/// It is undefined behavior when both File and Directory is set to non-null value.
/// </summary>
public class FileOrDirectory
{
    [YamlMember]
    public string? File;

    [YamlMember(Alias = "procContent")]
    public bool ProcessContent = false;

    [YamlMember(Alias = "dir")]
    public string? Directory;
}

/// <summary>
/// Controls how a folder should be exported for composition.
/// It is meant to be used with export.yml YAML files.
/// </summary>
public class ExportManifest
{
    [YamlMember]
    public List<FileOrDirectory> Link = new();
    
    [YamlMember]
    public List<FileOrDirectory> Copy = new();
}