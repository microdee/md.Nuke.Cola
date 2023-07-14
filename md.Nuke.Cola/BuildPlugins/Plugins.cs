using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Serilog;

namespace Nuke.Cola.BuildPlugins;

public delegate int MainExecute<T>(params Expression<Func<T, Target>>[] defaultTargetExpressions) where T : NukeBuild, new();

public static class Plugins
{
    private const string OutputBuildClass = nameof(OutputBuildClass);

    private const string ExecuteWithPlugins = nameof(ExecuteWithPlugins);

    private static string? GetCSharpName(Type type) =>
        type.Namespace == null ? type.Name : type.FullName;

    public static int Execute<T>(MainExecute<T> defaultExecute) where T : NukeBuild, new()
    {
        BuildContext context = new(NukeBuild.TemporaryDirectory, NukeBuild.RootDirectory);
        var engines = new []
        {
            new CSharpScriptPluginProvider()
        };
        foreach(var engine in engines)
        {
            engine.InitializeEngine(context);
        }

        var sources = engines
            .SelectMany(e => e.GatherPlugins(context))
            .ForEachLazy(p => Console.WriteLine($"Found build plugin at {p.SourcePath}"))
            .ToList();

        if (sources.IsEmpty())
        {
            Console.WriteLine($"No build plugins were found. Executing build from {typeof(T).Name}");
            return defaultExecute();
        }

        foreach(var source in sources)
        {
            source.Compile(context);
        }

        var buildInterfaces = sources.SelectMany(s => s.BuildInterfaces);
        var assemblyPaths = buildInterfaces
            .DistinctBy(i => i.ToString());

        var dllRefs = string.Join(
            Environment.NewLine,
            assemblyPaths
                .Select(p => p.ImportViaSource
                    ? $"#load \"{p}\""
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

        var intermediateScriptPath = context.Temporary / "Intermediate.csx";
        File.WriteAllText(intermediateScriptPath, intermediateScriptSource);
        var intermediateAssembliesRoot = context.Temporary / "IntermediateAssemblies";
        intermediateAssembliesRoot.CreateOrCleanDirectory();

        Console.WriteLine("Preparing intermediate assembly");
        var intermediateAssembly = Assembly.LoadFrom(
            DotnetScript.CompileDll(
                intermediateScriptPath,
                intermediateAssembliesRoot,
                context.Temporary
            )
        );

        var intermediateClass = intermediateAssembly.GetTypes().First(t => t.Name == OutputBuildClass);
        return (int) intermediateClass?.GetMethod(ExecuteWithPlugins)?.Invoke(null, null)!;
    }
}