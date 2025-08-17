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
/// <param name="ManifestFilePattern">
/// An explicit glob for finding export manifest files in the wprking~ and its subdirectories
/// </param>
/// <param name="Manifest">
/// Control how the contents of the folder should be imported into target project. If null and if
/// an export.yml manifest file exists in the imported folder, that manifest file will be used.
/// If both this parameter is null and an export.yml doesn't exist, then the folder will be simply
/// symlinked.
/// </param>
/// <returns></returns>
public record ImportFolderItem(AbsolutePath From, AbsolutePath ToParent, ExportManifest? Manifest = null, string ManifestFilePattern = "export.y*ml")
{
    public static implicit operator ImportFolderItem ((AbsolutePath from, AbsolutePath toParent) from)
        => new(from.from, from.toParent);

    public static implicit operator ImportFolderItem ((AbsolutePath from, AbsolutePath toParent, ExportManifest manifest) from)
        => new(from.from, from.toParent, from.manifest);

    public static implicit operator ImportFolderItem ((AbsolutePath from, AbsolutePath toParent, string manifestFilePattern) from)
        => new(from.from, from.toParent, ManifestFilePattern: from.manifestFilePattern);

    public static implicit operator ImportFolderItem ((AbsolutePath from, AbsolutePath toParent, ExportManifest manifest, string manifestFilePattern) from)
        => new(from.from, from.toParent, from.manifest, from.manifestFilePattern);

    public static implicit operator (AbsolutePath, AbsolutePath) (ImportFolderItem from)
        => (from.From, from.ToParent);

    public static implicit operator (AbsolutePath, AbsolutePath, ExportManifest?) (ImportFolderItem from)
        => (from.From, from.ToParent, from.Manifest);

    public static implicit operator (AbsolutePath, AbsolutePath, ExportManifest?, string) (ImportFolderItem from)
        => (from.From, from.ToParent, from.Manifest, from.ManifestFilePattern);
}

/// <summary>
/// Further options for ImportFolder
/// </summary>
/// <param name="UseSubfolder">
/// When and by default true, a subfolder is created for the import, or when false, the folder
/// is composited with the given target folder directly.
/// </param>
/// <param name="Pretend">
/// When true, file system actions are not invoked. This is useful for querying which files/folders
/// would be handled by this operation. Logging is also disabled while pretending
/// </param>
/// <param name="Suffixes">
/// Can be the desired project suffix, <see cref="ImportFolderSuffixes"/> for more details
/// </param>
public record ImportOptions(
    bool UseSubfolder = true,
    bool Pretend = false,
    ImportFolderSuffixes? Suffixes = null
);

public enum ImportMethod
{
    Copy,
    Link
}

public record ImportedItem(AbsolutePath From, AbsolutePath To, ImportMethod Method);

public static class FolderComposition
{
    private static string ProcessSuffix(this string target, ImportFolderSuffixes? suffixes, string leads = "_.:")
    {
        if (suffixes == null) return target;
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
        ImportFolderSuffixes? suffixes,
        AbsolutePath? until = null,
        string leads = "_.:"
    ) {
        if (suffixes == null) return target;
        if (until == null)
            return target.Parent / target.Name.ProcessSuffix(suffixes, leads);
        var relative = until.GetRelativePathTo(target).ToString();
        return until / relative.ProcessSuffix(suffixes, leads);
    }

    private static void ProcessSuffixContent(this AbsolutePath target, ImportFolderSuffixes? suffixes, string leads = "_.:")
    {
        if (suffixes == null) return;
        if (target.FileExists())
        {
            target.WriteAllText(
                target.ReadAllText().ProcessSuffix(suffixes, leads)
            );
        }
    }

    /// <summary>
    /// Convenience method for specifying multiple folder from/to pairs for <see cref="ImportFolder"/>
    /// <code>
    /// this.ImportFolders(
    ///     , new ImportOptions(Suffixes: "Test")
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
    /// </code>
    /// </summary>
    /// <param name="self">For easier access this is an extension method</param>
    /// <param name="options">
    /// When and by default true, a subfolder is created for the import, or when false, the folder
    /// is composited with the given target folder directly.
    /// </param>
    /// <param name="imports">
    /// The folder import from / to pair. Optionally can specify an export manifest.
    /// <see cref="ImportFolderItem"/>
    /// </param>
    public static List<ImportedItem> ImportFolders(this INukeBuild self, ImportOptions? options, params ImportFolderItem[] imports)
        => [.. imports.SelectMany(i => ImportFolder(self, i, options))];
    
    /// <inheritdoc cref="ImportFolders(INukeBuild, ImportOptions?, ImportFolderItem[])"/>
    public static List<ImportedItem> ImportFolders(this INukeBuild self, params ImportFolderItem[] imports)
        => [.. imports.SelectMany(i => ImportFolder(self, i))];

    /// <summary>
    /// There are cases when one project needs to compose from one pre-existing rigid folder
    /// structure of one dependency to another rigid folder structure of the current project. For
    /// scenarios like this Nuke.Cola provides this extension method which will copy/link the target
    /// folder and its contents according to some instructions expressed by either an `export.yml`
    /// file in the imported folder or provided explicitly from <see cref="ImportFolderItem"/>.
    /// </summary>
    /// <param name="self">For easier access this is an extension method</param>
    /// <param name="import">
    /// The folder import from / to pair. Optionally can specify an export manifest.
    /// <see cref="ImportFolderItem"/>
    /// </param>
    /// <param name="options">
    /// When and by default true, a subfolder is created for the import, or when false, the folder
    /// is composited with the given target folder directly.
    /// </param>
    public static List<ImportedItem> ImportFolder(
        this INukeBuild self,
        ImportFolderItem import,
        ImportOptions? options = null
    ) {
        options ??= new();
        var manifestPath = import.From.GetFiles(import.ManifestFilePattern).FirstOrDefault();
        var to = options.UseSubfolder ? import.ToParent / import.From.Name.ProcessSuffix(options.Suffixes) : import.ToParent;
        var instructions = import.Manifest ?? manifestPath?.ReadYaml<ExportManifest>();
        var result = new List<ImportedItem>();
        
        if (instructions == null)
        {
            if (!options.Pretend) to.LinksDirectory(import.From);
            result.Add(new(import.From, to, ImportMethod.Link));
            return result;
        }

        if (!to.DirectoryExists() && !options.Pretend) to.CreateDirectory();

        void FileSystemTask(
            IEnumerable<FileOrDirectory> list,
            Func<AbsolutePath, AbsolutePath, FileOrDirectory, ImportedItem> handleDirectories,
            Func<AbsolutePath, AbsolutePath, FileOrDirectory, ImportedItem> handleFiles
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
                            result.Add(handleDirectories(p, dst.ProcessSuffixPath(options.Suffixes, to), glob));
                        });
                else
                    import.From.SearchFiles(glob.File!)
                        .ForEach((p, i) =>
                        {
                            var dst = glob.GetDestination(import.From, to, p, i, exclude);
                            if (dst == null) return;
                            result.Add(handleFiles(p, dst.ProcessSuffixPath(options.Suffixes, to), glob));
                        });
            }
        }

        FileSystemTask(
            instructions.Copy,
            (src, dst, glob) =>
            {
                if (!options.Pretend) src.Copy(dst, ExistsPolicy.MergeAndOverwrite);
                return new(src, dst, ImportMethod.Copy);
            },
            (src, dst, glob) =>
            {
                if (!options.Pretend)
                {
                    src.Copy(dst, ExistsPolicy.FileOverwrite);
                    if (glob.ProcessContent)
                        dst.ProcessSuffixContent(options.Suffixes);
                }
                return new(src, dst, ImportMethod.Copy);
            }
        );

        FileSystemTask(
            instructions.Link,
            (src, dst, glob) =>
            {
                if (!options.Pretend) dst.LinksDirectory(src);
                return new(src, dst, ImportMethod.Link);
            },
            (src, dst, glob) =>
            {
                if (!options.Pretend) dst.LinksFile(src);
                return new(src, dst, ImportMethod.Link);
            }
        );

        foreach (var glob in instructions.Use)
        {
            if (string.IsNullOrWhiteSpace(glob.Directory) && string.IsNullOrWhiteSpace(glob.File))
                continue;

            var manifestFilePattern = glob.ManifestFilePattern ?? import.ManifestFilePattern;

            var manifestGlob = glob.Directory != null
                ? glob.Directory + "/" + manifestFilePattern
                : glob.File;

            var manifests = import.From.SearchFiles(manifestGlob!)
                    .Select(p => p.Parent)
                    .Where(p => p != import.From)
                    .ToList();

            if (!options.Pretend)
            {
                if (manifests.Count > 0)
                    Log.Information(
                        "Folder {0} uses {1} importing\n    {2}",
                        import.From, manifestGlob,
                        string.Join("\n    ", manifests)
                    );
                else
                    Log.Warning(
                        "Folder {0} attempted to import {1} but no importable subfolders were found (none of them had an export manifest file)",
                        import.From, manifestGlob
                    );
            }

            var exclude = glob.Not.Concat(instructions.Not);

            manifests.ForEach((p, i) =>
            {
                var dst = glob.GetDestination(import.From, to, p, i, exclude);
                if (dst == null)
                {
                    if (!options.Pretend) Log.Information("Ignoring folder {0}", p, dst);
                    return;
                }
                if (!options.Pretend) Log.Information("Importing folder {0} -> {1}", p, dst);
                result.AddRange(
                    self.ImportFolder(
                        (p, dst.Parent, Path.GetFileName(manifestGlob!)),
                        options with { UseSubfolder = true }
                    )
                );
            });
        }

        return result;
    }

    /// <summary>
    /// The result of ImportFolder only contain directory references if they were imported
    /// explicitly as a directory as a singular item (so not via file globbing). However there are
    /// cases in which all affected files should be minded. WithFilesExpanded converts the result
    /// of ImportFolder into a proper flat list of files only. It can work with pretended data as
    /// well, maintaining correct relations between From / To members.
    /// </summary>
    /// <param name="importedItems"></param>
    /// <param name="pattern"></param>
    /// <param name="depth"></param>
    /// <returns>
    /// A flat list of all files affected even in directories referenced as singular items.
    /// </returns>
    public static IEnumerable<ImportedItem> WithFilesExpanded(
        this IEnumerable<ImportedItem> importedItems,
        string pattern = "*",
        int depth = 40
    ) => importedItems
        .SelectMany(i =>
        {
            if (i.From.DirectoryExists())
                return i.From.GetFiles(pattern, depth)
                    .Select(f => new ImportedItem(
                        f, i.To / i.From.GetRelativePathTo(f), i.Method
                    ))
                ;
            else return [i];
        });
}