using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GlobExpressions;
using Nuke.Cola.Search;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Utilities.Collections;
using Nuke.Utilities.Text.Yaml;

namespace Nuke.Cola.FolderComposition;

public record ImportFolderSuffixes(string To, string From = "Origin")
{
    public static implicit operator ImportFolderSuffixes (string from)
        => new(from);

    public static implicit operator ImportFolderSuffixes ((string to, string from) from)
        => new(from.to, from.from);

    public static implicit operator (string, string) (ImportFolderSuffixes from)
        => (from.To, from.From);
}

public record ImportFolderItem(AbsolutePath From, AbsolutePath ToParent, ExportManifest? Manifest = null)
{
    public static implicit operator ImportFolderItem ((AbsolutePath from, AbsolutePath toParent) from)
        => new(from.from, from.toParent);

    public static implicit operator ImportFolderItem ((AbsolutePath from, AbsolutePath toParent, ExportManifest manifest) from)
        => new(from.from, from.toParent, from.manifest);

    public static implicit operator (AbsolutePath, AbsolutePath) (ImportFolderItem from)
        => (from.From, from.ToParent);

    public static implicit operator (AbsolutePath, AbsolutePath, ExportManifest?) (ImportFolderItem from)
        => (from.From, from.ToParent, from.Manifest);
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

    private static AbsolutePath ProcessSuffixPath(
        this AbsolutePath target,
        ImportFolderSuffixes suffixes,
        AbsolutePath? until = null,
        string leads = "_.:"
    ) {
        if (until == null)
            return target.Parent / target.Name.ProcessSuffix(suffixes, leads);
        var relative = until.GetRelativePathTo(target).ToString();
        return until / relative.ProcessSuffix(suffixes, leads);
    }

    private static void ProcessSuffixContent(this AbsolutePath target, ImportFolderSuffixes suffixes, string leads = "_.:")
    {
        if (target.FileExists())
        {
            target.WriteAllText(
                target.ReadAllText().ProcessSuffix(suffixes, leads)
            );
        }
    }

    public static void ImportFolders(this INukeBuild self, ImportFolderSuffixes suffixes, params ImportFolderItem[] imports)
        => imports.ForEach(i => ImportFolder(self, suffixes, i));

    public static void ImportFolder(this INukeBuild self, ImportFolderSuffixes suffixes, ImportFolderItem import)
    {
        var manifestPath = (import.From / "export.yml").ExistingFile()
            ?? import.From / "export.yaml";

        var to = import.ToParent / import.From.Name.ProcessSuffix(suffixes);

        var instructions = import.Manifest ?? manifestPath.ExistingFile()?.ReadYaml<ExportManifest>();
        
        if (instructions == null)
        {
            to.LinksDirectory(import.From);
            return;
        }

        if (!to.DirectoryExists()) to.CreateDirectory();

        void FileSystemTask(
            IEnumerable<FileOrDirectory> list,
            Action<AbsolutePath, AbsolutePath, FileOrDirectory> handleDirectories,
            Action<AbsolutePath, AbsolutePath, FileOrDirectory> handleFiles
        ) {
            foreach (var glob in list)
            {
                if (string.IsNullOrWhiteSpace(glob.Directory) && string.IsNullOrWhiteSpace(glob.File))
                    continue;
                
                if (string.IsNullOrWhiteSpace(glob.File))
                    import.From.SearchDirectories(glob.Directory!)
                        .ForEach(p =>
                        {
                            var dst = to / import.From.GetRelativePathTo(p);
                            handleDirectories(p, dst.ProcessSuffixPath(suffixes, to), glob);
                        });
                else
                    import.From.SearchFiles(glob.File!)
                        .ForEach(p =>
                        {
                            var dst = to / import.From.GetRelativePathTo(p);
                            handleFiles(p, dst.ProcessSuffixPath(suffixes, to), glob);
                        });
            }
        }

        FileSystemTask(
            instructions.Copy,
            (src, dst, glob) => FileSystemTasks.CopyDirectoryRecursively(
                src, dst, DirectoryExistsPolicy.Merge, FileExistsPolicy.Overwrite
            ),
            (src, dst, glob) => {
                FileSystemTasks.CopyFile(src, dst, FileExistsPolicy.Overwrite);
                if (glob.ProcessContent)
                    dst.ProcessSuffixContent(suffixes);
            }
        );

        FileSystemTask(
            instructions.Link,
            (src, dst, glob) => dst.LinksDirectory(src),
            (src, dst, glob) => dst.LinksFile(src)
        );
    }
}