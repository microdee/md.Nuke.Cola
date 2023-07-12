using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.Utilities;

namespace md.Nuke.Cola;

public class TextExtensions
{
    public static string ProcessArgument(string arg)
    {
        if(string.IsNullOrWhiteSpace(arg)) return arg;

        arg = arg.TrimMatchingDoubleQuotes()
            .Replace("''", "\"") // sequence for double quotes
            .Replace("~-", "-"); // sequence for -
        if(arg[0] == '~')
            arg = string.Concat("-", arg.AsSpan(1));
        return arg;
    }

    public static IEnumerable<string> AsArguments(this IEnumerable<string> args) =>
        args?.Select(ProcessArgument) ?? Enumerable.Empty<string>();

    public static string AppendAsArguments(this IEnumerable<string> input, bool leadingSpace = true) =>
        (input?.IsEmpty() ?? true)
            ? ""
            : (leadingSpace ? " " : "") + string.Join(' ', input?.Select(ProcessArgument) ?? Enumerable.Empty<string>());

    public static IEnumerable<string> DoubleQuoteIfNeeded(this IEnumerable<object> self) =>
        self?.Select(s => s.ToString().DoubleQuoteIfNeeded()) ?? Enumerable.Empty<string>();

    public static Func<string, string?> Parse(this string? input, string pattern)
    {
        if (input == null) return i => null;
        
        var groups = Regex.Matches(input, pattern)?.FirstOrDefault()?.Groups;
        return i => groups?[i]?.Value;
    }
}