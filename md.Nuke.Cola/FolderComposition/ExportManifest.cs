using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;
using YamlDotNet.Serialization;

public class FileOrDirectory
{
    [YamlMember]
    public string? File;

    [YamlMember(Alias = "procContent")]
    public bool ProcessContent = false;

    [YamlMember(Alias = "dir")]
    public string? Directory;
}

public class ExportManifest
{
    [YamlMember]
    public List<FileOrDirectory> Link = new();
    
    [YamlMember]
    public List<FileOrDirectory> Copy = new();
}