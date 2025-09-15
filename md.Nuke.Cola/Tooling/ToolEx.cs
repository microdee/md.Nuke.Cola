using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Serilog;
using Serilog.Events;

namespace Nuke.Cola.Tooling;

/// <summary>
/// Extended copy of Tool delegate of Nuke
/// </summary>
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

/// <summary>
/// A record listing Tool and ToolEx delegate parameters and provides a way to meaningfully merge multiple together
/// </summary>
/// <param name="ToolArgs">Regular Tool delegate arguments</param>
/// <param name="Input">Handle standard input stream after process creation</param>
public record ToolExArguments(
    ToolArguments ToolArgs,
    Action<StreamWriter>? Input = null
) {
    
    /// <summary>
    /// Merge two ToolEx argument records together.
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
    /// <item><term>Input </term><description> A + B is invoked</description></item>
    /// </list>
    /// </remarks>
    public static ToolExArguments operator | (ToolExArguments? a, ToolExArguments? b)
        => new(a?.ToolArgs | b?.ToolArgs, a?.Input + b?.Input);
    
    /// <summary>
    /// Merge a ToolEx and a Tool argument record together.
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
    /// <item><term>Input </term><description> Used from ToolEx arguments</description></item>
    /// </list>
    /// </remarks>
    public static ToolExArguments operator | (ToolExArguments? a, ToolArguments? b)
        => new(a?.ToolArgs | b, a?.Input);
    
    /// <summary>
    /// Merge a Tool and a ToolEx argument record together.
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
    /// <item><term>Input </term><description> Used from ToolEx arguments</description></item>
    /// </list>
    /// </remarks>
    public static ToolExArguments operator | (ToolArguments? a, ToolExArguments? b)
        => new(a | b?.ToolArgs, b?.Input);
}

/// <summary>
/// Propagated ToolEx delegate provider for launch parameter composition.
/// </summary>
/// <param name="Target"></param>
/// <param name="PropagateArguments"></param>
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