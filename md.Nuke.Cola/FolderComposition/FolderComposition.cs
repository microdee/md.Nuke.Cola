using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GlobExpressions;
using Nuke.Cola.Search;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Nuke.Utilities.Text.Yaml;
using Serilog;

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
    /// <param name="useSubfolder">
    /// When and by default true, a subfolder is created for the import, or when false, the folder
    /// is composited with the given target folder directly.
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
    public static void ImportFolders(this INukeBuild self, ImportFolderSuffixes suffixes, bool useSubfolder, params ImportFolderItem[] imports)
        => imports.ForEach(i => ImportFolder(self, suffixes, i, useSubfolder));
    
    /// <inheritdoc cref="ImportFolders(INukeBuild, ImportFolderSuffixes, bool, ImportFolderItem[])"/>
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
    /// <param name="useSubfolder">
    /// When and by default true, a subfolder is created for the import, or when false, the folder
    /// is composited with the given target folder directly.
    /// </param>
    public static void ImportFolder(this INukeBuild self, ImportFolderSuffixes suffixes, ImportFolderItem import, bool useSubfolder = true)
    {
        var manifestPath = (import.From / "export.yml").ExistingFile()
            ?? import.From / "export.yaml";

        var to = useSubfolder ? import.ToParent / import.From.Name.ProcessSuffix(suffixes) : import.ToParent;

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

                var exclude = glob.Not.Concat(instructions.Not);
                
                if (string.IsNullOrWhiteSpace(glob.File))
                    import.From.SearchDirectories(glob.Directory!)
                        .ForEach((p, i) =>
                        {
                            var dst = glob.GetDestination(import.From, to, p, i, exclude);
                            if (dst == null) return;
                            handleDirectories(p, dst.ProcessSuffixPath(suffixes, to), glob);
                        });
                else
                    import.From.SearchFiles(glob.File!)
                        .ForEach((p, i) =>
                        {
                            var dst = glob.GetDestination(import.From, to, p, i, exclude);
                            if (dst == null) return;
                            handleFiles(p, dst.ProcessSuffixPath(suffixes, to), glob);
                        });
            }
        }

        FileSystemTask(
            instructions.Copy,
            (src, dst, glob) => src.Copy(dst, ExistsPolicy.MergeAndOverwrite),
            (src, dst, glob) => {
                src.Copy(dst, ExistsPolicy.FileOverwrite);
                if (glob.ProcessContent)
                    dst.ProcessSuffixContent(suffixes);
            }
        );

        FileSystemTask(
            instructions.Link,
            (src, dst, glob) => dst.LinksDirectory(src),
            (src, dst, glob) => dst.LinksFile(src)
        );

        foreach (var glob in instructions.Use)
        {
            if (string.IsNullOrWhiteSpace(glob.Directory) && string.IsNullOrWhiteSpace(glob.File))
                continue;

            var manifestGlob = glob.Directory != null
                ? glob.Directory + "/export.y*ml"
                : glob.File;

            var manifests = import.From.SearchFiles(manifestGlob!)
                    .Select(p => p.Parent)
                    .Where(p => p != import.From)
                    .ToList();

            if (manifests.Count > 0)
                Log.Information(
                    "Folder {0} uses {1} importing\n    {2}",
                    import.From, manifestGlob,
                    string.Join("\n    ", manifests)
                );
            else
                Log.Warning(
                    "Folder {0} attempted to import {1} but no importable subfolders were found (none of them had export.yml manifest file)",
                    import.From, manifestGlob
                );

            var exclude = glob.Not.Concat(instructions.Not);

            manifests.ForEach((p, i) =>
            {
                var dst = glob.GetDestination(import.From, to, p, i, exclude);
                if (dst == null)
                {
                    Log.Information("Ignoring folder {0}", p, dst);
                    return;
                }
                Log.Information("Importing folder {0} -> {1}", p, dst);
                self.ImportFolder(suffixes, (p, dst.Parent));
            });
        }
    }
}