using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Build.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Serilog;

namespace Nuke.Cola.BuildPlugins;

/// <summary>
/// Delegate for the `Execute` entrypoint NUKE provides for build classes
/// </summary>
public delegate int MainExecute<T>(params Expression<Func<T, Target>>[] defaultTargetExpressions) where T : NukeBuild, new();

/// <summary>
/// This class contains the main entry point for builds supporting plugins.
/// </summary>
public static class Plugins
{
    private const string OutputBuildClass = nameof(OutputBuildClass);

    private const string ExecuteWithPlugins = nameof(ExecuteWithPlugins);
    private const string ReuseCompiled = nameof(ReuseCompiled);

    private static string? GetCSharpName(Type type)
        => type.Namespace == null ? type.Name : type.FullName;

    private static bool AttemptReuseCompiledPlugins()
        => EnvironmentInfo.HasArgument(ReuseCompiled)
        || EnvironmentInfo.GetVariable<int>("REUSE_COMPILED") == 1;

    /// <summary>
    ///     Use this instead of the regular Execute method in your main function if you want to support
    ///     build plugins.
    /// </summary>
    /// <param name="defaultExecute">
    ///     If no build plugins are found, the provided delegate will be executed instead
    /// </param>
    /// <typeparam name="T">The type of the main build class</typeparam>
    /// <returns>The error code or 0 on success</returns>
    /// <remarks>
    ///     If plugins are found they're collected into an intermediate C# script which defines a
    ///     build class inheriting from the provided main build class and implementing all the build
    ///     interfaces defined by each plugin. This however also means build interfaces cannot have
    ///     non-default-implemented members, so they behave more like composition in this case.
    ///     
    ///     The main NUKE execute method is then called from within this intermediate class.
    ///     These plugins can then interact with the main build targets if they can reference to
    ///     the main build assembly, either directly or more elegantly through a Nuget package.
    ///
    ///     Use `--ReuseCompiledPlugins` argument or `REUSE_COMPILED
    /// </remarks>
    public static int Execute<T>(MainExecute<T> defaultExecute) where T : NukeBuild, new()
    {
        BuildContext context = new(NukeBuild.TemporaryDirectory, NukeBuild.RootDirectory);
        var intermediateScriptPath = context.Temporary / "Intermediate.csx";
        var intermediateAssembliesRoot = context.Temporary / "IntermediateAssemblies";
        
        if (AttemptReuseCompiledPlugins())
        {
            if ((context.Temporary / "NoPlugins.txt").FileExists())
            {
                $"Skipped building build plugins entirely. Executing build from {typeof(T).Name}".Log();
                return defaultExecute();
            }

            if (intermediateScriptPath.FileExists())
            {
                var (dllPath, _, _) = DotnetCommon.GetDllLocationOfScript(
                    intermediateScriptPath,
                    intermediateAssembliesRoot
                );
                if (dllPath.FileExists())
                {
                    "Reusing plugins which has been already compiled.".Log();
                    return ExecuteExternal(dllPath);
                }
            }
        }
        
        var engines = new IProvidePlugins[]
        {
            new ImplicitBuildInterfacePluginProvider(),
            new CSharpScriptPluginProvider(),
            new DotnetProjectPluginProvider()
        };
        
        foreach(var engine in engines)
        {
            engine.InitializeEngine(context);
        }

        var sources = engines
            .SelectMany(e => e.GatherPlugins(context))
            .ForEachLazy(p => $"Using plugin {p.SourcePath}".Log())
            .ToList();

        if (sources.IsEmpty())
        {
            $"No build plugins were found. Executing build from {typeof(T).Name}".Log();
            return defaultExecute();
        }

        sources.AsParallel().ForAll(s => s.Compile(context));

        var buildInterfaces = sources.SelectMany(s => s.BuildInterfaces);
        var assemblyPaths = buildInterfaces
            .DistinctBy(i => i.ToString());

        var dllRefs = string.Join(
            Environment.NewLine,
            assemblyPaths
                .Where(p => p.Source != null)
                .Select(p => p.ImportViaSource
                    ? $"\n// dll: {p.Interface.Assembly.Location}\n#load \"{p}\""
                    : $"#r \"{p}\""
                )
        );
        var interfaces = string.Join(", ", buildInterfaces.Select(i => GetCSharpName(i.Interface)));
        var baseName = GetCSharpName(typeof(T));
        var currentAssembly = Assembly.GetEntryAssembly()?.Location;

        Assert.NotNull(currentAssembly);

        var intermediateScriptSource =
            $$"""
            #r "nuget: System.Linq.Expressions, 4.3.0"
            #r "{{currentAssembly}}"
            {{dllRefs}}
            
            public class {{OutputBuildClass}} : {{baseName}}, {{interfaces}}
            {
                public static int {{ExecuteWithPlugins}}() => Execute<{{OutputBuildClass}}>();
            }
            """;

        File.WriteAllText(intermediateScriptPath, intermediateScriptSource);
        // intermediateAssembliesRoot.CreateOrCleanDirectory();

        "Preparing intermediate assembly".Log();
        return ExecuteExternal(
            DotnetCommon.CompileScript(intermediateScriptPath, intermediateAssembliesRoot)
        );
    }

    private static int ExecuteExternal(AbsolutePath dllPath)
    {
        var intermediateAssembly = Assembly.LoadFrom(dllPath);

        var intermediateClass = intermediateAssembly.GetTypes().First(t => t.Name == OutputBuildClass);
        return (int) intermediateClass?.GetMethod(ExecuteWithPlugins)?.Invoke(null, null)!;
    }
}