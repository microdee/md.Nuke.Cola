# Build Plugins {#BuildPlugins}

[TOC]

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

## ImplicitBuildInterface plugins

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

## nuke.csx C# script plugins

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

## Nuke.csproj C# project plugins

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

## Disable re-discovering plugins

Building plugins can take a long time, and if nuke is run repeatedly this can get worse pretty quickly. For this reason if `REUSE_COMPILED` environment variable is set to 1 or `--ReuseCompiled` is present in command line arguments, plugins are only re-built / re-discovered the first time they're needed. Consecutive runs assume that nothing is changed.

This is useful for CI runs or when nuke is run frequently locally.

As an extra Nuke.Cola provides `build.ps1` / `build.sh` entry points which also respect these indicators, so they can skip directly to executing the build without going through all the checks and preparations.