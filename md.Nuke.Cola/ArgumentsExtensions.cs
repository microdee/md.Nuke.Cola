using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Cola;

public static class ArgumentsExtensions
{
    public static string? ProcessArgument(string? arg)
    {
        if(string.IsNullOrWhiteSpace(arg)) return arg;

        arg = arg.TrimMatchingDoubleQuotes()
            .Replace("''", "\"") // sequence for double quotes
            .Replace("~-", "-"); // sequence for -
        if(arg[0] == '~')
            arg = string.Concat("-", arg.AsSpan(1));
        return arg;
    }

    public static IEnumerable<string?> AsArguments(this IEnumerable<string>? args)
        => args?.Select(ProcessArgument) ?? Enumerable.Empty<string?>();

    public static string AppendAsArguments(this IEnumerable<string>? input, bool leadingSpace = true)
        => (input?.IsEmpty() ?? true)
            ? ""
            : (leadingSpace ? " " : "") + string.Join(' ', input?.Select(ProcessArgument) ?? Enumerable.Empty<string>());
}