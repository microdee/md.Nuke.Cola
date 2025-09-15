using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Serilog;
using Serilog.Events;

namespace Nuke.Cola.Tooling;

public delegate IReadOnlyCollection<Output>? ToolEx(
    // Nuke Tool
    ArgumentStringHandler arguments = default,
    string? workingDirectory = null,
    IReadOnlyDictionary<string, string>? environmentVariables = null,
    int? timeout = null,
    bool? logOutput = null,
    bool? logInvocation = null,
    Action<OutputType, string>? logger = null,
    Action<IProcess>? exitHandler = null,
    
    // Extension
    Action<StreamWriter>? input = null
);

public record ToolExArguments(
    ToolArguments ToolArgs,
    Action<StreamWriter>? Input = null
) {
    public static ToolExArguments operator | (ToolExArguments? a, ToolExArguments? b)
        => new(a?.ToolArgs | b?.ToolArgs, a?.Input + b?.Input);
    
    public static ToolExArguments operator | (ToolExArguments? a, ToolArguments? b)
        => new(a?.ToolArgs | b, a?.Input);
    
    public static ToolExArguments operator | (ToolArguments? a, ToolExArguments? b)
        => new(a | b?.ToolArgs, b?.Input);
}

public record PropagateToolExExecution(ToolEx Target, ToolExArguments? PropagateArguments = null)
{
    public IReadOnlyCollection<Output>? Execute(
        // Nuke Tool
        ArgumentStringHandler arguments = default,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        int? timeout = null,
        bool? logOutput = null,
        bool? logInvocation = null,
        Action<OutputType, string>? logger = null,
        Action<IProcess>? exitHandler = null,

        // Extension
        Action<StreamWriter>? input = null
    ) => Target.ExecuteWith(
        PropagateArguments | new ToolExArguments(
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
            input
        )
    );
}

internal class ToolExExecutor
{
    private static readonly char[] s_pathSeparators = { EnvironmentInfo.IsWin ? ';' : ':' };
    private static readonly object s_lock = new();
    
    private readonly string _toolPath;

    public ToolExExecutor(string toolPath)
    {
        _toolPath = toolPath;
    }

    public IReadOnlyCollection<Output>? Execute(
        // Nuke Tool
        ArgumentStringHandler arguments = default,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        int? timeout = null,
        bool? logOutput = null,
        bool? logInvocation = null,
        Action<OutputType, string>? logger = null,
        Action<IProcess>? exitHandler = null,

        // Extension
        Action<StreamWriter>? input = null
    )
    {
        workingDirectory ??= EnvironmentInfo.WorkingDirectory;
        logInvocation ??= true;
        logOutput ??= true;
        logger ??= ProcessTasks.DefaultLogger;
        var outputFilter = arguments.GetFilter();

        var toolPath = _toolPath;
        var args = arguments.ToStringAndClear();
        
        if (!Path.IsPathRooted(_toolPath) && !_toolPath.Contains(Path.DirectorySeparatorChar))
            toolPath = ToolPathResolver.GetPathExecutable(_toolPath);
        
        var toolPathOverride = GetToolPathOverride(toolPath);
        if (!string.IsNullOrEmpty(toolPathOverride))
        {
            args = $"{toolPath.DoubleQuoteIfNeeded()} {args}".TrimEnd();
            toolPath = toolPathOverride;
        }
        
        Assert.FileExists(toolPath);
        Assert.DirectoryExists(workingDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = toolPath,
            Arguments = args,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = input != null,
            UseShellExecute = false,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardInputEncoding = Encoding.UTF8,
        };
        
        if (environmentVariables != null)
        {
            startInfo.Environment.Clear();
            foreach (var (key, value) in environmentVariables)
                startInfo.Environment[key] = value;
        }

        if (logInvocation.Value)
            LogInvocation(startInfo, outputFilter);

        var process = Process.Start(startInfo);
        if (process == null)
            return null;
        
        input?.Invoke(process.StandardInput);
        
        var output = GetOutputCollection(process, logger, outputFilter);
        var proc2 = new Process2(process, outputFilter, timeout, output);
        
        (exitHandler ?? (p => p.AssertZeroExitCode())).Invoke(proc2.AssertWaitForExit());
        return proc2.Output;
    }
    
    private static string? GetToolPathOverride(string toolPath)
    {
        if (toolPath.EndsWithOrdinalIgnoreCase(".dll"))
        {
            return ToolPathResolver.TryGetEnvironmentExecutable("DOTNET_EXE") ??
                   ToolPathResolver.GetPathExecutable("dotnet");
        }

        if (EnvironmentInfo.IsUnix &&
            toolPath.EndsWithOrdinalIgnoreCase(".exe") &&
            !EnvironmentInfo.IsWsl)
            return ToolPathResolver.GetPathExecutable("mono");

        return null;
    }
    
    private static BlockingCollection<Output> GetOutputCollection(
        Process process,
        Action<OutputType, string>? logger,
        Func<string, string> outputFilter)
    {
        var output = new BlockingCollection<Output>();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null)
                return;

            var filteredOutput = outputFilter(e.Data);
            output.Add(new Output { Text = filteredOutput, Type = OutputType.Std });
            logger?.Invoke(OutputType.Std, filteredOutput);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null)
                return;

            var filteredOutput = outputFilter(e.Data);
            output.Add(new Output { Text = filteredOutput, Type = OutputType.Err });
            logger?.Invoke(OutputType.Err, filteredOutput);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return output;
    }
    
    private static void LogInvocation(ProcessStartInfo startInfo, Func<string, string> outputFilter)
    {
        lock (s_lock)
        {
            Log.Information("> {ToolPath} {Arguments}", startInfo.FileName.DoubleQuoteIfNeeded(), outputFilter(startInfo.Arguments));
            Log.Write(
                startInfo.WorkingDirectory != EnvironmentInfo.WorkingDirectory
                    ? LogEventLevel.Information
                    : LogEventLevel.Verbose,
                "@ {WorkingDirectory}",
                startInfo.WorkingDirectory);
        }
    }
}