using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Cola;

/// <summary>
/// Extension class for dealing with passing arguments from the user through nuke to a tool
/// </summary>
public static class Arguments
{
    /// <summary>
    /// Unescape argument input which is passed by the user through a parameter
    /// </summary>
    public static string? ProcessArgument(string? arg)
    {
        if (string.IsNullOrWhiteSpace(arg)) return arg;

        arg = arg.TrimMatchingDoubleQuotes()
            .Replace("''", "\"") // sequence for double quotes
            .Replace("~-", "-"); // sequence for -
        if (arg[0] == '~')
            arg = string.Concat("-", arg.AsSpan(1));
        return arg;
    }

    /// <summary>
    /// Unescape multiple argument input which is passed by the user through a parameter
    /// </summary>
    public static IEnumerable<string?> AsArguments(this IEnumerable<string>? args)
        => args?.Select(ProcessArgument) ?? Enumerable.Empty<string?>();

    /// <summary>
    /// Unescape multiple argument input which is passed by the user through a parameter
    /// </summary>
    public static string AppendAsArguments(this IEnumerable<string>? input, bool leadingSpace = true)
        => (input?.IsEmpty() ?? true)
            ? ""
            : (leadingSpace ? " " : "") + string.Join(' ', input?.Select(ProcessArgument) ?? Enumerable.Empty<string>());

    /// <summary>
    /// Gets an optionally named block of arguments. An argument block starts with "-->" (+ optional
    /// name) and either ends at the start of another argument block or ends at the last argument.
    /// <para>
    /// For example
    /// </para>
    /// <code>
    /// > nuke display-args --param1 foo --> -d foo bar /switch="asdasd"
    ///   args: -d foo bar /switch="asdasd"
    /// > nuke display-args --param1 foo -->b1 -d foo bar -->b2 /switch="asdasd"
    ///   b1 args: -d foo bar
    ///   b2 args: /switch="asdasd"
    /// </code>
    /// </summary>
    /// <remarks>
    /// Nuke allows unknown arguments for its reflection system, but it doesn't know it should stop
    /// processing arguments inside an argument block. If the build has similarly named arguments
    /// it may interfere with other parameters. Design your build usage with this in mind.
    /// </remarks>
    public static IEnumerable<string> GetBlock(string name = "", IEnumerable<string>? from = null)
    {
        var args = from ?? EnvironmentInfo.CommandLineArguments;
        return args
            .SkipUntil(a => a == "-->" + name)
            .Skip(1)
            .TakeUntil(a => a.StartsWith("-->"));
    }

    /// <summary>
    /// Gets an optionally named block of arguments. An argument block starts with "-->" (+ optional
    /// name) and either ends at the start of another argument block or ends at the last argument.
    /// <para>
    /// For example
    /// </para>
    /// <code>
    /// > nuke display-args --param1 foo --> -d foo bar /switch="asdasd"
    ///   args: -d foo bar /switch="asdasd"
    /// > nuke display-args --param1 foo -->b1 -d foo bar -->b2 /switch="asdasd"
    ///   b1 args: -d foo bar
    ///   b2 args: /switch="asdasd"
    /// </code>
    /// </summary>
    /// <remarks>
    /// Nuke allows unknown arguments for its reflection system, but it doesn't know it should stop
    /// processing arguments inside an argument block. If the build has similarly named arguments
    /// it may interfere with other parameters. Design your build usage with this in mind.
    /// </remarks>
    public static IEnumerable<string> GetArgumentBlock(this IEnumerable<string> from, string name = "") => GetBlock(name, from);
}