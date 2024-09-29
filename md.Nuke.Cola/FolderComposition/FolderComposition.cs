using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GlobExpressions;
using Nuke.Cola.BuildPlugins;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Utilities.Collections;
using Nuke.Utilities.Text.Yaml;

namespace Nuke.Cola.FolderComposition;

public record ImportFolderSuffixes(string To, string From = "Origin")
{
    public static implicit operator ImportFolderSuffixes (string from)
        => new(from);

    public static implicit operator ImportFolderSuffixes (ValueTuple<string, string> from)
        => new(from.Item1, from.Item2);

    public static implicit operator ValueTuple<string, string> (ImportFolderSuffixes from)
        => (from.To, from.From);
}

public record ImportFolderItem(AbsolutePath From, AbsolutePath ToParent)
{
    public static implicit operator ImportFolderItem (ValueTuple<AbsolutePath, AbsolutePath> from)
        => new(from.Item1, from.Item2);

    public static implicit operator ValueTuple<AbsolutePath, AbsolutePath> (ImportFolderItem from)
        => (from.From, from.ToParent);
}

public static class FolderComposition
{
    private static string ProcessSuffix(this string target, ImportFolderSuffixes suffixes, string leads = "_.:")
    {
        string result = target;
        foreach (char lead in leads)
        {
            result = result.Replace(
                lead + suffixes.From, lead + suffixes.To,
                true, CultureInfo.InvariantCulture
            );
        }
        return result;
    }

    private static AbsolutePath ProcessSuffixPath(this AbsolutePath target, ImportFolderSuffixes suffixes, string leads = "_.:")
        => target.Parent / target.Name.ProcessSuffix(suffixes, leads);

    private static void ProcessSuffixContent(this AbsolutePath target, ImportFolderSuffixes suffixes, string leads = "_.:")
    {
        if (target.FileExists())
        {
            target.WriteAllText(
                target.ReadAllText().ProcessSuffix(suffixes, leads)
            );
        }
    }

    public static void ImportFolders(this NukeBuild self, ImportFolderSuffixes suffixes, params ImportFolderItem[] imports)
        => imports.ForEach(import => ImportFolder(self, suffixes, import));

    public static void ImportFolder(this NukeBuild self, ImportFolderSuffixes suffixes, ImportFolderItem import)
    {
        var manifestPath = (import.From / "export.yml").ExistingFile()
            ?? import.From / "export.yaml";

        var to = import.ToParent.ProcessSuffixPath(suffixes);
        
        if (!manifestPath.FileExists())
        {
            if (!to.DirectoryExists()) Directory.CreateSymbolicLink(to, import.From);
            return;
        }

        var instructions = manifestPath.ReadYaml<ExportManifest>();

        if (!to.DirectoryExists()) to.CreateDirectory();

        void FileSystemTask(
            IEnumerable<FileOrDirectory> list,
            Action<AbsolutePath, AbsolutePath> handleDirectories,
            Action<AbsolutePath, AbsolutePath> handleFiles
        ) {
            foreach (var glob in list)
            {
                if (string.IsNullOrWhiteSpace(glob.Directory) && string.IsNullOrWhiteSpace(glob.File))
                    continue;
                
                if (string.IsNullOrWhiteSpace(glob.File))
                    import.From.SearchDirectories(glob.Directory!)
                        .ForEach(p =>
                        {
                            var dst = to / p.GetRelativePathTo(import.From);
                            handleDirectories(p, to.ProcessSuffixPath(suffixes));
                        });
                else
                    import.From.SearchFiles(glob.File!)
                        .ForEach(p =>
                        {
                            var dst = to / p.GetRelativePathTo(import.From);
                            handleFiles(p, to.ProcessSuffixPath(suffixes));
                        });
            }
        }

        FileSystemTask(
            instructions.Copy,
            (src, dst) => FileSystemTasks.CopyDirectoryRecursively(
                src, dst, DirectoryExistsPolicy.Merge, FileExistsPolicy.Overwrite
            ),
            (src, dst) => {
                FileSystemTasks.CopyFile(src, dst, FileExistsPolicy.Overwrite);
                dst.ProcessSuffixContent(suffixes);
            }
        );

        FileSystemTask(
            instructions.Link,
            (src, dst) => Directory.CreateSymbolicLink(dst, src),
            (src, dst) => File.CreateSymbolicLink(dst, src)
        );
    }
}