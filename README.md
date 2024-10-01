- [Nuke.Cola](#nukecola)
  - [Build Plugins](#build-plugins)
    - [`[ImplicitBuildInterface]` plugins](#implicitbuildinterface-plugins)
    - [`*.nuke.csx` C# script plugins](#nukecsx-c-script-plugins)
    - [`*.Nuke.csproj` C# project plugins](#nukecsproj-c-project-plugins)
  - [Folder Composition](#folder-composition)
    - [Regular folders](#regular-folders)
    - [Folders with export manifest](#folders-with-export-manifest)
  - [Build GUI (WIP)](#build-gui-wip)


# Nuke.Cola

Utilities and extensions useful for any Nuke builds originally separated from Nuke.Unreal.

Name comes from Nuka Cola of the Fallout franchise.

## Build Plugins

Build plugins for Nuke is a way to implicitly share build tasks which the main build tool can pick up automatically from anywhere within the project. For components which are independent of each-other but they don't individually make a complete project, like a composition of those are expressing the resulting piece of software, it might not make sense for them to have independent full fledged Nuke context for each of them. For these scenarios Nuke.Cola provides a way to discover C# scripts or C# projects following a specific convention anywhere inside the subfolders of the project (recursively). These plugins can be then just distributed and re-used along these components.

<details><summary>Practical justification / reasoning / motivation</summary>

This is originally developed for Unreal plugins where the main project's Nuke scripts or the pre-built independently distributed build-tools shouldn't explicitly know about the plugin composition of the project they need to work on in runtime. Unreal, or other non-dotnet project models might not have the capacity or cultural acceptance to rely on a Nuget infrastructure to distribute build scripts of these independent software components, which then could be referenced by the main project. Even if that would be plausable it would still take uncomfortable extra boilerplate for each of these software components. In case of a pre-built build tool based on a Nuke build script, this is the only way I know of to have dynamically composable software component specific build scripts considered.

</details>


Build plugins are discovered before the main entry point of Nuke, if the developer uses

```CSharp
public static int Main () => Plugins.Execute<Build>(Execute);
```

<details><summary>instead of the traditional</summary>

```CSharp
public static int Main () => Execute<Build>(x => x.Compile);
```

</details>

> [!NOTE]
> * Your main Build class needs to be `public` for this to work.
> * Currently it is not yet implemented to support an explicit default target, so when Nuke is executed without arguments, it will just print the help text.

The following kinds of plugins discovered this way:

* `*.nuke.csx` standalone C# script files.
* `*.Nuke.csproj` named C# projects.
* `[ImplicitBuildInterface]` tagged interfaces in the main build.

Scripts are better when there's a single file with few targets which don't need to interact with the main build class, projects are better for more elaborate scenarios and `[ImplicitBuildInterface]` can be used when `<Compile Include="../**/*.nuke.cs" />` is specified for the build project.

In all cases build interfaces inheriting `INukeBuild` are picked up and their targets and parameters are added to the final Nuke build class. [Read more about Nuke build interfaces](https://nuke.build/docs/sharing/build-components/) (or "Build Components" as they call it). Targets of Plugins have complete freedom to interact with the entire build graph, especially when the build graph is expressed first in a Nuget package library (like [Nuke.Unreal](https://github.com/microdee/Nuke.Unreal) already gives Unreal plugins a lot to work with).

### `[ImplicitBuildInterface]` plugins

This is simply an interface defined in your main build project. It may not seem very useful until one factors in the following addition to your build csproj:

```XML
  <ItemGroup>
    <Compile Include="../**/*.nuke.cs" />
  </ItemGroup>
```

This means that without any further configuration one can put `.nuke.cs` files anywhere in their project and write scripts in the context of their placement. This is the easiest to configure method requiring the least bboilerplate but obviously it doesn't work on pre-built nuke build-tools.

<details><summary>except...</summary>

if one creates a `csproj` based build plugin which sole purpose is to include source files that way. In that case the prebuilt build tool can discover `.nuke.cs` files provided when compiling the `csproj` plugin.

</details>

Example:

```CSharp
using Nuke.Common;
using Nuke.Unreal;
using Nuke.Cola;
using Serilog;

[ImplicitBuildInterface]
public interface IExtraTargets : INukeBuild
{
    Target TestPlugin => _ => _
        .DependentFor<UnrealBuild>(b => b.Generate)
        .Executes(() =>
        {
            Log.Information($"Hello from folder {this.ScriptFolder()}");
        });
}
```

<details><summary>Open for detailed explanation:</summary>

```CSharp
using Nuke.Common;
using Nuke.Unreal;
using Nuke.Cola;
using Serilog;

// This attribute is necessary so other optional build components wouldn't get used unexpectedly
[ImplicitBuildInterface]
// The build component interface should only declare members with default implementations
// as there's no manual way to provide those in the implementing intermediate build class.
public interface IExtraTargets : INukeBuild
{
    // Define your targets or parameters freely and connect them with the build graph
    // OR the developer can explicitly call them with `nuke test-plugin` in this case
    Target TestPlugin => _ => _

        // Automatically run this target before the Generate target is invoked.
        // `UnrealBuild` is a base build class providing common Unreal related targets and
        // parameters, including `Generate`.
        .DependentFor<UnrealBuild>(b => b.Generate)

        // Finally declare what this target should actually do when invoked
        .Executes(() =>
        {
            // Use this.ScriptFolder() to work with this file's location
            Log.Information($"Hello from folder {this.ScriptFolder()}");
        });
}
```

[Read more about target definitions in NUKE.](https://nuke.build/docs/fundamentals/targets/)

</details>

### `*.nuke.csx` C# script plugins

Example:

```CSharp
#r "nuget: md.Nuke.Unreal, 2.0.5"

using Nuke.Common;
using Nuke.Unreal;
using Serilog;

public interface IExtraTargets : INukeBuild
{
    Target TestPlugin => _ => _
        .DependentFor<UnrealBuild>(b => b.Generate)
        .Executes(() =>
        {
            Log.Information($"Hello from folder {this.ScriptFolder()}");
        });
}
```

<details><summary>Open for detailed explanation:</summary>

```CSharp
#r "nuget: md.Nuke.Unreal, 2.0.5"

using Nuke.Common;
using Nuke.Unreal;
using Serilog;

// The build component interface should only declare members with default implementations
// as there's no manual way to provide those in the implementing intermediate build class.
public interface IExtraTargets : INukeBuild
{
    // Define your targets or parameters freely and connect them with the build graph
    // OR the developer can explicitly call them with `nuke test-plugin` in this case
    Target TestPlugin => _ => _

        // Automatically run this target before the Generate target is invoked.
        // `UnrealBuild` is a base build class providing common Unreal related targets and
        // parameters, including `Generate`.
        .DependentFor<UnrealBuild>(b => b.Generate)

        // Finally declare what this target should actually do when invoked
        .Executes(() =>
        {
            // Use this.ScriptFolder() to work with this file's location
            Log.Information($"Hello from folder {this.ScriptFolder()}");
        });
}
```

[Read more about target definitions in NUKE.](https://nuke.build/docs/fundamentals/targets/)

</details>

You can put a `*.nuke.csx` file anywhere and it will be picked up as a Build Plugin. `Nuke.Cola` will also configure VSCode for C# scripts auto-completion support as common courtasy. In order for VSCode to pick up nuget references use `.NET: Restart Language Server` via the command palette (or `Omnisharp: Restart OmniSharp` in case that fallback is used). Debugging plugins require you to modify `.vscode/launch.json` and run the desired targets/parameters as startup. Mutliple scripts with the same name in different folders can co-exist as long as they define unique interface names.

### `*.Nuke.csproj` C# project plugins

You can put a `*.Nuke.csproj` named project anywhere and the build script using `Nuke.Cola` will pick it up. Simplest way to do it is via dotnet command line:

```
> dotnet new classlib --name MyPlugin.Nuke
```

then add `Nuke.Cola` Nuget package (and your own project's specific shared build components):

```
> cd .\MyPlugin.Nuke
> dotnet package add md.Nuke.Cola
```

and then you can proceed as with any other dotnet class library.

> [!NOTE]
> Unlike scripts, each C# project build plugin needs to be named uniquely in one project.

## Folder Composition

There are cases when one project needs to compose from one pre-existing rigid folder structure of one dependency to another rigid folder structure of the current project. For scenarios like this Nuke.Cola provides `ImportFolder` build class extension method which will copy/link the target folder and its contents according to some instructions expressed by either an `export.yml` file in the imported folder or provided explicitly to the `ImportFolder` extension method.

<details><summary>Practical justification / reasoning / motivation</summary>

In Unreal Engine a code plugin can only be uploaded to the Marketplace (or now Fab) if it doesn't depend on any other code plugins or anything else outside of the archive the seller provides Epic for distribution. This makes however sharing code between these plugins non-trivial.

Simply duplicating code among plugins is good enough until the user acquires two or more of these plugins using shared code with the same module names. Unreal modules names need to be unique in a global scope because Epic doesn't like namespaces for pragmatic reasons. To work around this while allowing duplicated code to be referenced in Unreal projects without ambiguity I cooked up an automatic solution which may need lot's of explanation but provides easy setup maintaining freedom for modularity.

Folder composition allows couple of more things:

* Maintaining the above mentioned shared code in a central location without that needing to know where they might be shared
* Linking to only couple of subfolders of a submoduled monorepo in your root project.

</details>

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

            this.ImportFolders("Test"
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

### Regular folders

A regular folder (like `Unassuming`) with no extra import instructions provided will be just symlinked.

Folders and files containing a preset suffix (by default `Origin` with either `_`, `.` and `:` as separators) can be replaced by a suffix chosen by the importing script (in this case `Test`). Files/Folders linked or copied will have this suffix replaced at their destination. (see `FolderOnly_Origin` -> `FolderOnly_Test`)

### Folders with export manifest

Folders can dictate the intricacies of how they're shared this way with a simple manifest file called `export.yml`. For example the one in `Both_Test` does

```yaml
link:
  - dir: Private/SharedSubfolder
  - dir: Public/SharedSubfolder
copy:
  - file: "**/*_Origin.*"
    procContent: true
```

Linking folders is straightforward and globbable. Target folders and parent folders (up until export root) is processed for suffixes at destination same as when copying folders.

When linking files, globbed files are individually symlinked with suffix processing on file/folder name.

Copying folders recursively have the same folder name processing but doesn't touch its contents (not even file/folder name processing)

When copying files (including when they're globbed) their content can be also processed for suffixes if `procContent` is set to true. Copying an entire folder recursively but with all the file/folder names processed can be done by simply doing recursive globbing `-file: "MyFolder/**"`. The reasoning behind this design is suggesting performance implications, that each file is individually treated.

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

## Build GUI (WIP)

Build scripts can get complex enough that it is hard to fisrt grasp the options it can give to the user especially ones which dynamically import Build Plugins. Of course we have `--help` and the `--plan` features Nuke provides, but a nice interactive UI can help much more with team adoption, especially one which shows relations of which parameters are being used by which Nuke Target.

So far this is only there to aid Nuke adoption and select targets and related parameters to run for people who're not familiar with Nuke yet. More visualizations to mirror target relations and support more Nuke goodies might come in a distant future™.

