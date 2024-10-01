using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Serilog;

namespace Nuke.Cola.Tooling;

public static class ToolColaExtensions
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
}