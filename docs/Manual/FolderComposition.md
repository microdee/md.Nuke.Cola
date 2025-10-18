# Folder Composition {#FolderComposition}

[TOC]

There are cases when one project needs to compose from one pre-existing rigid folder structure of one dependency to another rigid folder structure of the current project. For scenarios like this Nuke.Cola provides `ImportFolder` build class extension method which will copy/link the target folder and its contents according to some instructions expressed by either a YAML manifest file (by default `export.yml`) in the imported folder or provided explicitly to the `ImportFolder` extension method.

For example the following target:

```CSharp
[ImplicitBuildInterface]
public interface IImportTestFolders : INukeBuild
{
    Target ImportTestFolders => _ => _
        .Executes(() => 
        {
            var root = this.ScriptFolder();
            var target = root / "Target";
            var thirdparty = root / "ThirdParty";

            this.ImportFolders(
                new ImportOptions(Suffixes: "Test")
                , (thirdparty / "Unassuming", target)
                , (thirdparty / "FolderOnly_Origin", target)
                , (thirdparty / "WithManifest" / "Both_Origin", target / "WithManifest")
                , (thirdparty / "WithManifest" / "Copy_Origin", target / "WithManifest")
                , (thirdparty / "WithManifest" / "Link_Origin", target / "WithManifest")
            );
        });
}
```

will process/import the file/folder structure on the left to the file/folder structure on the right.

```
ThirdParty                                  ->  Target
├───FolderOnly_Origin                       ->  ├───FolderOnly_Test <symlink>
│       SomeFile_B.txt                          │       *
├───Unassuming                              ->  ├───Unassuming <symlink>
│       SomeFile_A.txt                          │       *
└───WithManifest                            ->  └───WithManifest
    ├───Both_Origin                         ->      ├───Both_Test
    │   │   export.yml                              │   │   -
    │   │   SomeModule_Origin.build.txt     ->      │   │   SomeModule_Test.build.txt
    │   ├───ExcludedFolder                          │   │   -
    │   │       SomeFile_Excluded.txt               │   │       -
    │   ├───Private                         ->      │   ├───Private
    │   │   │   ModuleFile_Origin.cpp.txt   ->      │   │   │   ModuleFile_Test.cpp.txt
    │   │   └───SharedSubfolder             ->      │   │   └───SharedSubfolder <symlink>
    │   │           SomeFile.cpp.txt                │   │           *
    │   └───Public                          ->      │   └───Public
    │       └───SharedSubfolder             ->      │       └───SharedSubfolder <symlink>
    │               SomeFile.h.txt                  │               *
    ├───Copy_Origin                         ->      ├───Copy_Test
    │   │   export.yml                              │   │   -
    │   ├───Foo                             ->      │   ├───Foo
    │   │   │   Foo.bar                     ->      │   │   │   Foo.bar
    │   │   └───Bar_Origin                  ->      │   │   └───Bar_Test
    │   │           A.txt                   ->      │   │           A.txt
    │   └───Wizzard                         ->      │   └───Wizzard
    │       │   B.txt                       ->      │       │   B.txt
    │       └───Ech                         ->      │       └───Ech_Test
    │               C_Origin.txt            ->      │               C_Test.txt
    └───Link_Origin                         ->      └───Link_Test
        │   export.yml                                  │   -
        ├───Foo                             ->          ├───Foo
        │   │   Foo.bar                     ->          │   │   Foo.bar
        │   └───Bar_Origin                  ->          │   └───Bar_Test <symlink>
        │           A.txt                               │           *
        └───Wizzard                         ->          └───Wizzard
            │   B_Origin.txt                ->              │   B_Test.txt <symlink>
            └───Ech_Origin                  ->              └───Ech_Test
                    C_Origin.txt            ->                      C_Test.txt <symlink>
```

> [!NOTE]
> The resulting file/folder structure is also controlled by the `export.yml` files which content is not explicitly spelled out here, and is discussed further below.

To break it down:

## Regular folders

A regular folder (like `Unassuming`) with no extra import instructions provided will be just symlinked.

Optionally folders and files containing a preset suffix (for example and by default `Origin` with either `_`, `.` and `:` as separators) can be replaced by another suffix chosen by the importing script (in this case `Test`). Files/Folders linked or copied will have this suffix replaced at their destination. (see `FolderOnly_Origin` -> `FolderOnly_Test`). If `Suffixes` were not specified in `ImportOptions` or if `ImportOptions` is not provided at all, suffix processing is skipped.

## Folders with export manifest

Folders can dictate the intricacies of how they're shared this way with a simple YAML manifest file (by default called `export.yml` but this can be changed). For example the one in `Both_Test` does

```yaml
link:
  - dir: Private/SharedSubfolder          # Simply link single subfolder at destination maintaining structure
  - dir: Public/SharedSubfolder           # Simply link single subfolder at destination maintaining structure
  - dir: Some/Deep/Folder/For/Some/Reason # Map a deep folder structure into a singular level
    as: MyNiceFolder
copy:
  - file: "**/*_Origin.*" # Individually and recursively copy every file with a suffix "_Origin" maintaining subfolder structure
    procContent: true     # also replace [_.:]Origin suffixes in the content of files (controlled by the importer)
```

Linking folders is straightforward and globbable. If suffixes are provided target folders and parent folders (up until export root) is processed for suffixes at destination same as when copying folders.

When linking files, globbed files are individually symlinked, optionally with suffix processing on file/folder name.

Copying folders recursively have the same folder name processing but doesn't touch its contents (not even file/folder name processing)

When copying files (including when they're globbed) their content can be also processed for suffixes if `procContent` is set to true and suffixes are enabled. Copying an entire folder recursively but with all the file/folder names processed can be done by simply doing recursive globbing `-file: "MyFolder/**"`. The reasoning behind this design is suggesting performance implications, that each file is individually treated.

The destination relative path and name can also be overridden with `as:`. This works with globbing as well where the captures of `*` and `**` can be referred to as `$N` where N is the 1 based index of the wildcards. For example

```yaml
- file: Flatten/**/*.txt # Select all text files arbitrarily deep inside subfolder structure
  as: Flatten/$2.txt     # copy/link them in a singular folder referencing the second wildcard
```

Where all the files inside the recursive structure of subfolder `Flatten` is copied/linked into a single-level subfolder. `$2` in `as:` indicates it uses the second wildcard (the one at `*.txt`).

Export manifests can reference other export manifests when they're listed under `use` key the same way as copies or links are done, for example:

```yaml
use:
  - dir: Subfolder/MyDependency         # will export subfolder to destination maintaining subfolder structure
  - dir: Components/*                   # consider using all direct subfolders inside components folder
  - dir: Components/**                  # consider using all recursive subfolders inside components folder
  - dir: Components/**/*                # consider using all recursive subfolders inside components folder BUT
    as: Components/$2                   # flatten them into a single subfolder
    manifestFilePattern: flat.yml       # and use `flat.yml` as export manifests
  - dir: "**"                           # just use everything from the recursive subfolder structure which has an export.yml (by default)
  - file: Another/Dependency/stuff.yml  # use an explicit file for the manifest being imported
```

This feature is not visualized above in the folder structure figure as that's already complicated enough. Note that `copy` and `link` will not consider instructions from export manifest files, only `use` does that, and `use` will ignore every folder which doesn't have an export manifest file directly in it. This is done this way to alleviate surprises from "smart defaults".

## Exclude/ignore items

There are cases when it's easier to have an explicit rule for ignoring files/folders from a generic match, instead of composing a match for wide range of files, but the ones we want to exclude. For this reason folder composition allows to list patterns which will exclude matched paths. They can be declared for all the exported folder, or individual glob items.

```YAML
copy:
  - file: Folder/**/*   # copy everything from Folder
    not:
      - ".*/"           # except dot folders
      - "*.generated.*" # except "generated" files
  - dir: Source         # above exceptions don't apply here
not:
  - Intermediate/       # Ignore intermediate folders in all entries.
```

> [!NOTE]
> For now "ignore files" like `.gitignore` are not considered, but in the future there may be a feature which ignores files the way git does with `.gitignore` upon request. Until that time copying entries from your gitignore to `not:` fields as an array should do the same, if that is indeed the preferred behavior.

<details><summary><b>NOTE</b> about string values in YAML containing *</summary>

`*` in YAML has special meaning. Therefore string values containing `*` needs to be single or double quoted. Therefore

```YAML
copy:
  - file: **/*_Origin.* # ERROR
```

```YAML
copy:
  - file: "**/*_Origin.*" # OK
  - file: '**/*_Origin.*' # OK
```

</details>