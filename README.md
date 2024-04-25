- [Nuke.Cola](#nukecola)
  - [Build Plugins](#build-plugins)
    - [`*.Nuke.csproj` C# project plugins](#nukecsproj-c-project-plugins)
    - [`*.nuke.csx` C# script plugins](#nukecsx-c-script-plugins)
  - [Build GUI (WIP)](#build-gui-wip)


# Nuke.Cola

Utilities and extensions useful for any Nuke builds originally separated from Nuke.Unreal.

Name comes from Nuka Cola of the Fallout franchise.

## Build Plugins

Build plugins for Nuke is a way to implicitly share build tasks which the main build tool can pick up automatically from anywhere within the project. For components which are independent of each-other but they don't individually make a complete project, like a composition of those are expressing the resulting piece of software, it might not make sense for them to have independent full fledged Nuke context for each of them. For these scenarios Nuke.Cola provides a way to discover C# scripts or C# projects following a specific convention anywhere inside the subfolders of the project (recursively). These plugins can be then just distributed and re-used along these components.

<details>

<summary>Practical justification / reasoning / motivation</summary>

This is originally developed for Unreal plugins where the main project's Nuke scripts or the pre-built independently distributed build-tools shouldn't explicitly know about the plugin composition of the project they need to work on in runtime. Unreal, or other non-dotnet project models might not have the capacity or cultural acceptance to rely on a Nuget infrastructure to distribute build scripts of these independent software components, which then could be referenced by the main project. Even if that would be plausable it would still take uncomfortable extra boilerplate for each of these software components. In case of a pre-built build tool based on a Nuke build script, this is the only way I know of to have dynamically composable software component specific build scripts considered.

</details>

Build plugins are discovered before the main entry point of Nuke, if the developer uses

```CSharp
public static int Main () => Plugins.Execute<Build>(Execute);
```

instead of the traditional

```CSharp
public static int Main () => Execute<Build>(x => x.Compile);
```

Note that currently it is not yet implemented to support an explicit default target, so when Nuke is executed without arguments, it will just print the help text.

There are two kinds of plugins discovered this way:

* `*.nuke.csx` standalone C# script files
* `*.Nuke.csproj` named C# projects.

Scripts are better when there's a single file with few targets and projects are better for more elaborate scenario.

In both cases build interfaces inheriting `INukeBuild` are picked up and their targets and parameters are added to the final Nuke build class. [Read more about Nuke build interfaces](https://nuke.build/docs/sharing/build-components/) (or "Build Components" as they call it). Targets of Plugins have complete freedom to interact with the entire build graph, especially when the build graph is expressed first in a Nuget package library (like [Nuke.Unreal](https://github.com/microdee/Nuke.Unreal) already gives Unreal plugins a lot to work with).

For example here's a plugin which prints "Hello from plugins" after the `Generate` target is executed.


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
            Log.Information("Hello from plugins");
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
            Log.Information("Hello from plugins");
        });
}
```

[Read more about target definitions in NUKE.](https://nuke.build/docs/fundamentals/targets/)

</details>

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

Note that unlike scripts, each C# project build plugin needs to be named uniquely in one project.

### `*.nuke.csx` C# script plugins

You can put a `*.nuke.csx` file anywhere and it will be picked up as a Build Plugin. `Nuke.Cola` will also configure VSCode for C# scripts auto-completion support as common courtasy. In order for VSCode to pick up nuget references use `.NET: Restart Language Server` via the command palette (or `Omnisharp: Restart OmniSharp` in case that fallback is used). Debugging plugins require you to modify `.vscode/launch.json` and run the desired targets/parameters as startup. Mutliple scripts with the same name in different folders can co-exist as long as they define unique interface names.

## Build GUI (WIP)

Build scripts can get complex enough that it is hard to fisrt grasp the options it can give to the user especially ones which dynamically import Build Plugins. Of course we have `--help` and the `--plan` features Nuke provides, but a nice interactive UI can help much more with team adoption, especially one which shows relations of which parameters are being used by which Nuke Target.

So far this is only there to aid Nuke adoption and select targets and related parameters to run for people who're not familiar with Nuke yet. More visualizations to mirror target relations and support more Nuke goodies might come in a distant future™.

