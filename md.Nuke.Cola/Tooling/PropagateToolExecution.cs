using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Serilog;

namespace Nuke.Cola.Tooling;

/// <summary>
/// Propagated Tool delegate provider for launch parameter composition.
/// </summary>
/// <param name="Target"></param>
/// <param name="PropagateArguments"></param>
public record class PropagateToolExecution(Tool Target, ToolArguments? PropagateArguments = null)
{
    public IReadOnlyCollection<Output> Execute(
        ArgumentStringHandler arguments = default,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        int? timeout = null,
        bool? logOutput = null,
        bool? logInvocation = null,
        Action<OutputType, string>? logger = null,
        Action<IProcess>? exitHandler = null
    ) => Target.ExecuteWith(
        PropagateArguments | new ToolArguments(
            arguments.ToStringAndClear(),
            workingDirectory,
            environmentVariables,
            timeout,
            logOutput,
            logInvocation,
            logger,
            exitHandler
        )
    );
}