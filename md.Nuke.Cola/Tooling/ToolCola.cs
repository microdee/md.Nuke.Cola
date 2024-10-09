using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuke.Common;
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
    /// Mark app output Debug/Info/Warning/Error based on its content rather than the stream
    /// they were added to.
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="filter"></param>
    /// <param name="normalOutputLogger"></param>
    public static Tool WithSemanticLogging(this Tool tool, Func<string, bool>? filter = null, Action<OutputType, string>? normalOutputLogger = null)
        => tool.With(logger: (t, l) =>
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
    public static ValueOrError<Tool> GetPathTool(string tool, Action? setup = null)
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

    /// <summary>
    /// Use a common tool and attempt to fetch it from popular program managers if it's not installed
    /// yet for the user  Optionally provide a manual setup or provide another tool which this one
    /// is bundled with (like `pip` comes with `python` or `npm` comew with `node`)
    /// </summary>
    /// <param name="tool">The name of the tool</param>
    /// <param name="version">Use a specific version or the latest if this is left null</param>
    /// <param name="wingetId">Use a fully qualified ID for Windows Winget if necessary</param>
    /// <param name="manualSetup">If specified try this manual setup first</param>
    /// <param name="comesWith">
    /// This tool should come bundled with another one. Like `comesWith: () => PythonTasks.Python`.
    /// </param>
    /// <returns>The Tool or an error if none of the sources managed to set it up</returns>
    public static ValueOrError<Tool> Use(
        string tool,
        string? version = null,
        string? wingetId = null,
        Action? manualSetup = null,
        Func<Tool>? comesWith = null
    ) => GetPathTool(tool, manualSetup)
            .ElseTrySetup(comesWith != null, tool, () => comesWith!())
            .ElseTrySetup(EnvironmentInfo.IsWin, tool, () =>
                WingetTasks.Winget($"install {wingetId ?? tool} {version.PrependNonEmpty("-v "):nq}")
            )
            .ElseTrySetup(EnvironmentInfo.IsOsx, tool, () =>
                ToolResolver.GetPathTool("brew")($"install {tool}{version.PrependNonEmpty("@")}")
            )
            // TODO: linux
        ;
}