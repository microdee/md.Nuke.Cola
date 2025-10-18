using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Serilog;

namespace Nuke.Cola.Tooling;

public static class ToolCola
{
    /// <summary>
    /// Execute a tool with the arguments provided by the input record.
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="args"></param>
    public static IReadOnlyCollection<Output> ExecuteWith(this Tool tool, ToolArguments args)
        => tool(
            $"{args.Arguments:nq}",
            args.WorkingDirectory,
            args.EnvironmentVariables,
            args.Timeout,
            args.LogOutput,
            args.LogInvocation,
            args.Logger,
            args.ExitHandler
        );
    
    /// <summary>
    /// Execute a tool with standard input with the arguments provided by the input record.
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="args"></param>
    public static IReadOnlyCollection<Output>? ExecuteWith(this ToolEx tool, ToolExArguments args)
        => tool(
            $"{args.ToolArgs.Arguments:nq}",
            args.ToolArgs.WorkingDirectory,
            args.ToolArgs.EnvironmentVariables,
            args.ToolArgs.Timeout,
            args.ToolArgs.LogOutput,
            args.ToolArgs.LogInvocation,
            args.ToolArgs.Logger,
            args.ToolArgs.ExitHandler,
            args.Input
        );

    /// <summary>
    /// Set individual Tool launching parameters and propagate the delegate further
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="args"></param>
    /// <remarks>
    /// <list>
    /// <item><term>Arguments </term><description> will be concatenated</description></item>
    /// <item><term>Working directory </term><description> B overrides the one from A but not when B doesn't have one</description></item>
    /// <item><term>Environmnent variables </term><description> will be merged</description></item>
    /// <item><term>TimeOut </term><description> will be maxed</description></item>
    /// <item><term>LogOutput </term><description> is OR-ed</description></item>
    /// <item><term>LogInvocation </term><description> is OR-ed</description></item>
    /// <item><term>Logger / ExitHandler </term><description>A + B is invoked</description></item>
    /// </list>
    /// </remarks>
    public static Tool With(this Tool tool, ToolArguments args)
        => new PropagateToolExecution(tool, args).Execute;

    /// <summary>
    /// Set individual Tool launching parameters and propagate the delegate further
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="args"></param>
    /// <remarks>
    /// <list>
    /// <item><term>Arguments </term><description> will be concatenated</description></item>
    /// <item><term>Working directory </term><description> B overrides the one from A but not when B doesn't have one</description></item>
    /// <item><term>Environmnent variables </term><description> will be merged</description></item>
    /// <item><term>TimeOut </term><description> will be maxed</description></item>
    /// <item><term>LogOutput </term><description> is OR-ed</description></item>
    /// <item><term>LogInvocation </term><description> is OR-ed</description></item>
    /// <item><term>Logger / ExitHandler </term><description>A + B is invoked</description></item>
    /// <item><term>Input </term><description>A + B is invoked</description></item>
    /// <item><term>Encodings </term><description> B overrides the one from A but not when B doesn't have one</description></item>
    /// </list>
    /// </remarks>
    public static ToolEx With(this ToolEx tool, ToolExArguments args)
        => new PropagateToolExExecution(tool, args).Execute;
    
    /// <summary>
    /// Set individual Tool launching parameters and propagate the delegate further
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="args"></param>
    /// <remarks>
    /// <list>
    /// <item><term>Arguments </term><description> will be concatenated</description></item>
    /// <item><term>Working directory </term><description> B overrides the one from A but not when B doesn't have one</description></item>
    /// <item><term>Environmnent variables </term><description> will be merged</description></item>
    /// <item><term>TimeOut </term><description> will be maxed</description></item>
    /// <item><term>LogOutput </term><description> is OR-ed</description></item>
    /// <item><term>LogInvocation </term><description> is OR-ed</description></item>
    /// <item><term>Logger / ExitHandler </term><description>A + B is invoked</description></item>
    /// <item><term>Input </term><description> Used from ToolEx arguments</description></item>
    /// <item><term>Encodings </term><description> Used from ToolEx arguments</description></item>
    /// </list>
    /// </remarks>
    public static ToolEx With(this ToolEx tool, ToolArguments args)
        => new PropagateToolExExecution(tool, new(args)).Execute;

    /// <summary>
    /// Set individual Tool launching parameters and propagate the delegate further
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="arguments"></param>
    /// <param name="workingDirectory"></param>
    /// <param name="environmentVariables"></param>
    /// <param name="timeout"></param>
    /// <param name="logOutput"></param>
    /// <param name="logInvocation"></param>
    /// <param name="logger"></param>
    /// <param name="exitHandler"></param>
    /// <remarks>
    /// <list>
    /// <item><term>Arguments </term><description> will be concatenated</description></item>
    /// <item><term>Working directory </term><description> B overrides the one from A but not when B doesn't have one</description></item>
    /// <item><term>Environmnent variables </term><description> will be merged</description></item>
    /// <item><term>TimeOut </term><description> will be maxed</description></item>
    /// <item><term>LogOutput </term><description> is OR-ed</description></item>
    /// <item><term>LogInvocation </term><description> is OR-ed</description></item>
    /// <item><term>Logger / ExitHandler </term><description>A + B is invoked</description></item>
    /// </list>
    /// </remarks>
    public static Tool With(
        this Tool tool,
        ArgumentStringHandler arguments = default,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        int? timeout = null,
        bool? logOutput = null,
        bool? logInvocation = null,
        Action<OutputType, string>? logger = null,
        Action<IProcess>? exitHandler = null
    ) => tool.With(new ToolArguments(
        arguments.ToStringAndClear(),
        workingDirectory,
        environmentVariables,
        timeout,
        logOutput,
        logInvocation,
        logger,
        exitHandler
    ));

    /// <summary>
    /// Set individual Tool launching parameters and propagate the delegate further
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="arguments"></param>
    /// <param name="workingDirectory"></param>
    /// <param name="environmentVariables"></param>
    /// <param name="timeout"></param>
    /// <param name="logOutput"></param>
    /// <param name="logInvocation"></param>
    /// <param name="logger"></param>
    /// <param name="exitHandler"></param>
    /// <param name="input">Handle standard input stream after process creation</param>
    /// <param name="standardOutputEncoding">Encoding for standard output. Default is UTF8 (with BOM)</param>
    /// <param name="standardInputEncoding">Encoding for standard input. Default is UTF8 (without BOM)</param>
    /// <remarks>
    /// <list>
    /// <item><term>Arguments </term><description> will be concatenated</description></item>
    /// <item><term>Working directory </term><description> B overrides the one from A but not when B doesn't have one</description></item>
    /// <item><term>Environmnent variables </term><description> will be merged</description></item>
    /// <item><term>TimeOut </term><description> will be maxed</description></item>
    /// <item><term>LogOutput </term><description> is OR-ed</description></item>
    /// <item><term>LogInvocation </term><description> is OR-ed</description></item>
    /// <item><term>Logger / ExitHandler </term><description>A + B is invoked</description></item>
    /// <item><term>Input </term><description>A + B is invoked</description></item>
    /// <item><term>Encodings </term><description> B overrides the one from A but not when B doesn't have one</description></item>
    /// </list>
    /// </remarks>
    public static ToolEx With(
        this ToolEx tool,
        ArgumentStringHandler arguments = default,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        int? timeout = null,
        bool? logOutput = null,
        bool? logInvocation = null,
        Action<OutputType, string>? logger = null,
        Action<IProcess>? exitHandler = null,
        Action<StreamWriter>? input = null,
        Encoding? standardOutputEncoding = null,
        Encoding? standardInputEncoding = null
    ) => tool.With(new ToolExArguments(
        new(
            arguments.ToStringAndClear(),
            workingDirectory,
            environmentVariables,
            timeout,
            logOutput,
            logInvocation,
            logger,
            exitHandler
        ),
        input,
        standardOutputEncoding,
        standardInputEncoding
    ));

    /// <summary>
    /// Mark app output Debug/Info/Warning/Error based on its content rather than the stream
    /// they were added to.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="normalOutputLogger"></param>
    public static ToolArguments SemanticLogging(Func<string, bool>? filter = null, Action<OutputType, string>? normalOutputLogger = null)
        => new(Logger: (t, l) =>
        {
            if (!(filter?.Invoke(l) ?? true)) return;

            if (l.ContainsAnyOrdinalIgnoreCase("success", "complete", "ready", "start", "***"))
            {
                Log.Information(l);
            }
            else if (l.ContainsOrdinalIgnoreCase("warning"))
            {
                Log.Warning(l);
            }
            else if (l.ContainsAnyOrdinalIgnoreCase("error", "fail"))
            {
                Log.Error(l);
            }
            else
            {
                if (normalOutputLogger != null)
                    normalOutputLogger(t, l);
                else
                {
                    Log.Debug(l);
                }
            }
        });

    /// <summary>
    /// Mark app output Debug/Info/Warning/Error based on its content rather than the stream
    /// they were added to.
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="filter"></param>
    /// <param name="normalOutputLogger"></param>
    public static Tool WithSemanticLogging(this Tool tool, Func<string, bool>? filter = null, Action<OutputType, string>? normalOutputLogger = null)
        => tool.With(SemanticLogging(filter, normalOutputLogger));

    /// <summary>
    /// Mark app output Debug/Info/Warning/Error based on its content rather than the stream
    /// they were added to.
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="filter"></param>
    /// <param name="normalOutputLogger"></param>
    public static ToolEx WithSemanticLogging(this ToolEx tool, Func<string, bool>? filter = null, Action<OutputType, string>? normalOutputLogger = null)
        => tool.With(SemanticLogging(filter, normalOutputLogger));

    /// <summary>
    /// ToolArguments for priming environment variables
    /// </summary>
    public static ToolArguments CurrentEnvironment => new(EnvironmentVariables: EnvironmentInfo.Variables);

    /// <summary>
    /// A more comfortable passing of environment variables. This will also pass on parent environment
    /// </summary>
    public static ToolArguments EnvVar(string key, object value, bool includeParentEnvironment = true)
    {
        var result = includeParentEnvironment
            ? EnvironmentInfo.Variables.ToDictionary()
            : new();
        
        result[key] = value.ToString();
        return new(EnvironmentVariables: result);
    }

    /// <summary>
    /// A more comfortable passing of environment variables. This will also pass on parent environment
    /// </summary>
    public static ToolArguments EnvVars(bool includeParentEnvironment, params (string key, object value)[] items)
    {
        var parent = includeParentEnvironment
            ? EnvironmentInfo.Variables.ToDictionary()
            : null;
        var itemsDict = items.ToDictionary(i => i.key, i => i.value!.ToString()!);
        return new(EnvironmentVariables: parent.Merge(itemsDict));
    }

    /// <summary>
    /// A more comfortable passing of environment variables. This will also pass on parent environment
    /// </summary>
    public static ToolArguments EnvVars(params (string key, object value)[] items) => EnvVars(true, items);

    /// <summary>
    /// A more comfortable passing of environment variables. This will also pass on parent environment
    /// </summary>
    public static Tool WithEnvVar(this Tool tool, string key, object value, bool includeParentEnvironment = true)
        => tool.With(EnvVar(key, value, includeParentEnvironment));

    /// <summary>
    /// A more comfortable passing of environment variables. This will also pass on parent environment
    /// </summary>
    public static Tool WithEnvVars(this Tool tool, bool includeParentEnvironment, params (string key, object value)[] items)
        => tool.With(EnvVars(includeParentEnvironment, items));
    
    /// <summary>
    /// A more comfortable passing of environment variables. This will also pass on parent environment
    /// </summary>
    public static Tool WithEnvVars(this Tool tool, params (string key, object value)[] items)
        => tool.With(EnvVars(items));

    /// <summary>
    /// Add an input path to this tool's PATH list. It won't be added if input path is already in there.
    /// </summary>
    public static Tool WithPathVar(this Tool tool, AbsolutePath path)
        => tool.WithEnvVar(
            "PATH",
            EnvironmentInfo.Paths.Union([ path.ToString() ]).JoinSemicolon()
        );
    
    /// <summary>
    /// A more comfortable passing of environment variables. This will also pass on parent environment
    /// </summary>
    public static ToolEx WithEnvVar(this ToolEx tool, string key, object value, bool includeParentEnvironment = true)
        => tool.With(EnvVar(key, value, includeParentEnvironment));

    /// <summary>
    /// A more comfortable passing of environment variables. This will also pass on parent environment
    /// </summary>
    public static ToolEx WithEnvVars(this ToolEx tool, params (string key, object value)[] items)
        => tool.With(EnvVars(items));

    /// <summary>
    /// A more comfortable passing of environment variables. This will also pass on parent environment
    /// </summary>
    public static ToolEx WithEnvVars(this ToolEx tool, bool includeParentEnvironment, params (string key, object value)[] items)
        => tool.With(EnvVars(includeParentEnvironment, items));

    /// <summary>
    /// Add an input path to this tool's PATH list. It won't be added if input path is already in there.
    /// </summary>
    public static ToolEx WithPathVar(this ToolEx tool, AbsolutePath path)
        => tool.WithEnvVar(
            "PATH",
            EnvironmentInfo.Paths.Union([ path.ToString() ]).JoinSemicolon()
        );

    /// <summary>
    /// Removes ANSI escape sequences from the output of a Tool (remove color data for example)
    /// </summary>
    public static IEnumerable<Output> RemoveAnsiEscape(this IEnumerable<Output> toolOutput)
        => toolOutput.Select(l => new Output
        {
            Type = l.Type,
            Text = l.Text.ReplaceRegex("\x1b\\[[0-9;]*[mK]", m => "")
        });

    /// <summary>
    ///     Pipe the results of a tool into the standard input of the next tool. This is not exactly the same as real
    ///     command line piping, the previous process needs to be finished first to pipe its output into the next one.
    ///     This however gives the opportunity to transform / filter the output of previous tool with regular LINQ
    ///     before passing it to the next one. 
    /// </summary>
    /// <param name="previous">The output of a previous program</param>
    /// <param name="next">Initial tool delegate of the next program</param>
    /// <param name="pipeError">Also pipe standard-error into next program</param>
    /// <param name="close">
    ///     If this is true, close the input stream after all the lines have been written. This is set to true by
    ///     default for ease of usage, as most of the time a program's output is the only thing needed to be passed to
    ///     another program. However if false don't forget to queue closing the input stream with CloseInput. 
    /// </param>
    /// <returns>A composite ToolEx delegate</returns>
    public static ToolEx Pipe(this IEnumerable<Output> previous, ToolEx next, bool pipeError = false, bool close = true)
        => next.With(input: s =>
        {
            foreach (var line in previous)
            {
                if (line.Type == OutputType.Std || pipeError)
                    s.WriteLine(line.Text);
            }
            if (close) s.Close();
        });

    /// <summary>
    /// Provide lines for standard input once the program is run. If the target program waits until end-of-stream
    /// queue closing the input stream with CloseInput.
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="lines"></param>
    public static ToolEx WithInput(this ToolEx tool, IEnumerable<string> lines)
        => tool.With(input: s =>
        {
            foreach (var line in lines)
                s.WriteLine(line);
        });

    /// <summary>
    /// Provide a single line for standard input once the program is run.
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="line"></param>
    public static ToolEx WithInput(this ToolEx tool, string line)
        => tool.With(input: s => s.WriteLine(line));
    
    /// <summary>
    /// Explicitly close the standard input after other inputs have been queued. Some programs may freeze without
    /// this step.
    /// </summary>
    /// <param name="tool"></param>
    public static ToolEx CloseInput(this ToolEx tool) => tool.With(input: s => s.Close());

    /// <summary>
    /// Attempt to update PATH of this process from user's environment variables
    /// </summary>
    public static void UpdatePathEnvVar()
    {
        if (!EnvironmentInfo.IsWin) return;
        var processPaths = EnvironmentInfo.Paths;
        var userPaths = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)!.Split(';');
        var machinePaths = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine)!.Split(';');

        var result = processPaths.Union(userPaths).Union(machinePaths).JoinSemicolon();
        Environment.SetEnvironmentVariable("PATH", result);
    }

    /// <summary>
    /// Get a tool which should be in PATH, and provide an optional way to set it up automatically if it wasn't
    /// </summary>
    /// <returns>The Tool or an error if it wasn't in PATH and the setup had failed</returns>
    public static ValueOrError<Tool> Use(string tool, Action? setup = null)
        => ErrorHandling.TryGet(() => ToolResolver.GetPathTool(tool))
            .Else(setup != null, () =>
            {
                Log.Warning($"{tool} was not installed, but it's OK we're installing it now.");
                setup!();
                UpdatePathEnvVar();
                return ToolResolver.GetPathTool(tool);
            });

    /// <summary>
    /// Try a different setup method for a Tool which may failed its installation
    /// </summary>
    /// <param name="result">Result of the previous attempt</param>
    /// <param name="condition">Only attempt this method of setup when condition is met</param>
    /// <param name="tool">The name of the tool</param>
    /// <param name="setup">Setup the tool for the caller</param>
    /// <returns>The Tool or an error if it this or previous setup attempts have failed</returns>
    public static ValueOrError<Tool> ElseTrySetup(this ValueOrError<Tool> result, bool condition, string tool, Action setup)
        => result.Else(condition, () =>
        {
            setup();
            UpdatePathEnvVar();
            return ToolResolver.GetPathTool(tool);
        });
        
    /// <summary>
    /// Try a different setup method for a Tool which may failed its installation
    /// </summary>
    /// <param name="result">Result of the previous attempt</param>
    /// <param name="tool">The name of the tool</param>
    /// <param name="setup">Setup the tool for the caller</param>
    /// <returns>The Tool or an error if it this or previous setup attempts have failed</returns>
    public static ValueOrError<Tool> ElseTrySetup(this ValueOrError<Tool> result, string tool, Action setup)
        => result.ElseTrySetup(true, tool, setup);
}