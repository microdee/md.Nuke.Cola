using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Serilog;

namespace Nuke.Cola.Tooling;

/// <summary>
/// A record listing Tool delegate parameters and provides a way to meaningfully merge multiple together
/// </summary>
/// <param name="Arguments"></param>
/// <param name="WorkingDirectory"></param>
/// <param name="EnvironmentVariables"></param>
/// <param name="Timeout"></param>
/// <param name="LogOutput"></param>
/// <param name="LogInvocation"></param>
/// <param name="Logger"></param>
/// <param name="ExitHandler"></param>
public record class ToolArguments(
    string? Arguments = null,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null,
    int? Timeout = null,
    bool? LogOutput = null,
    bool? LogInvocation = null,
    Action<OutputType, string>? Logger = null,
    Action<IProcess>? ExitHandler = null
) {

    /// <summary>
    /// Merge two Tool argument records together.
    /// </summary>
    /// <remarks>
    /// <list>
    /// <item><term>Arguments </term><description> will be concatenated</description></item>
    /// <item><term>Working directory </term><description> B overrides the one from A but not when B doesn't have one</description></item>
    /// <item><term>Environmnent variables </term><description> will be merged</description></item>
    /// <item><term>TimeOut </term><description> will be maxed</description></item>
    /// <item><term>LogOutput </term><description> is OR-ed</description></item>
    /// <item><term>LogInvocation </term><description> is OR-ed</description></item>
    /// <item><term>Logger / ExitHandler </term><description> A + B is invoked</description></item>
    /// </list>
    /// </remarks>
    public static ToolArguments operator | (ToolArguments? a, ToolArguments? b)
    {
        var timeOut = Math.Max(a?.Timeout ?? -1, b?.Timeout ?? -1);
        return new() {
            Arguments = string.Join(' ',
                new [] {a?.Arguments, b?.Arguments}
                    .Where(_ => !string.IsNullOrWhiteSpace(_))
            ),

            WorkingDirectory = string.IsNullOrWhiteSpace(b?.WorkingDirectory)
                ? a?.WorkingDirectory
                : b?.WorkingDirectory,
            
            EnvironmentVariables = a?.EnvironmentVariables.Merge(b?.EnvironmentVariables),

            Timeout = timeOut < 0 ? null : timeOut,

            LogOutput = (a?.LogOutput == null && b?.LogOutput == null)
                ? null
                : (a?.LogOutput ?? false) || (b?.LogOutput ?? false),

            LogInvocation = (a?.LogInvocation == null && b?.LogInvocation == null)
                ? null
                : (a?.LogInvocation ?? false) || (b?.LogInvocation ?? false),

            Logger = a?.Logger + b?.Logger,

            ExitHandler = a?.ExitHandler + b?.ExitHandler
        };
    }
}