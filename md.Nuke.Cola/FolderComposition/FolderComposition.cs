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

/// <summary>
/// A record for suffix replacement mapping
/// </summary>
/// <param name="To">The target suffix desired by the importing project</param>
/// <param name="From">The original suffix inside the source folder</param>
/// <returns></returns>
public record ImportFolderSuffixes(string To, string From = "Origin")
{
    public static implicit operator ImportFolderSuffixes (string from)
        => new(from);

    public static implicit operator ImportFolderSuffixes ((string to, string from) from)
        => new(from.to, from.from);

    public static implicit operator (string, string) (ImportFolderSuffixes from)
        => (from.To, from.From);
}

/// <summary>
/// A record importing a folder from a source to a parent folder
/// </summary>
/// <param name="From">The source or origin folder</param>
/// <param name="ToParent">The destination parent folder</param>
/// <param name="Manifest">
/// Control how the contents of the folder should be imported into target project. If null and if
/// an export.yml manifest file exists in the imported folder, that manifest file will be used.
/// If both this parameter is null and an export.yml doesn't exist, then the folder will be simply
/// symlinked.
/// </param>
/// <returns></returns>
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

    /// <summary>
    /// Convenience method for specifying multiple folder from/to pairs for <see cref="ImportFolder"/>
    /// </summary>
    /// <param name="self">For easier access this is an extension method</param>
    /// <param name="suffixes">
    /// Can be simply the desired project suffix, <see cref="ImportFolderSuffixes"/> for more details
    /// </param>
    /// <param name="imports">
    /// The folder import from / to pair. Optionally can specify an export manifest.
    /// <see cref="ImportFolderItem"/>
    /// </param>
    /// <example>
    /// this.ImportFolders("Test"
    ///     , (root / "Unassuming", target)
    ///     , (root / "FolderOnly_Origin", target)
    ///     , (root / "WithManifest" / "Both_Origin", target / "WithManifest")
    ///     , (root / "WithManifest" / "Copy_Origin", target / "WithManifest")
    ///     , (root / "WithManifest" / "Link_Origin", target / "WithManifest")
    ///     , (root / "ScriptControlled", target, new ExportManifest
    ///     {
    ///         Link = {
    ///             new() { Directory = "Private/SharedSubfolder"},
    ///             new() { Directory = "Public/SharedSubfolder"},
    ///         },
    ///         Copy = {
    ///             new() {
    ///                 File = "**/*_Origin.*",
    ///                 ProcessContent = true
    ///             }
    ///         }
    ///     })
    /// );
    /// </example>
    public static void ImportFolders(this INukeBuild self, ImportFolderSuffixes suffixes, params ImportFolderItem[] imports)
        => imports.ForEach(i => ImportFolder(self, suffixes, i));

    /// <summary>
    /// There are cases when one project needs to compose from one pre-existing rigid folder
    /// structure of one dependency to another rigid folder structure of the current project. For
    /// scenarios like this Nuke.Cola provides this extension method which will copy/link the target
    /// folder and its contents according to some instructions expressed by either an `export.yml`
    /// file in the imported folder or provided explicitly from <see cref="ImportFolderItem"/>.
    /// </summary>
    /// <param name="self">For easier access this is an extension method</param>
    /// <param name="suffixes">
    /// Can be simply the desired project suffix, <see cref="ImportFolderSuffixes"/> for more details
    /// </param>
    /// <param name="import">
    /// The folder import from / to pair. Optionally can specify an export manifest.
    /// <see cref="ImportFolderItem"/>
    /// </param>
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