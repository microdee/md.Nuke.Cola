using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Cola;

public static class TextExtensions
{
    public static IEnumerable<string> DoubleQuoteIfNeeded(this IEnumerable<object> self) =>
        self?.Select(s => s.ToString().DoubleQuoteIfNeeded()) ?? Enumerable.Empty<string>();

    public static Func<string, string?> Parse(this string? input, string pattern) =>
        Parse(input, new Regex(pattern));

    public static Func<string, string?> Parse(this string? input, Regex pattern)
    {
        if (input == null) return i => null;
        
        var groups = pattern.Matches(input)?.FirstOrDefault()?.Groups;
        return i => groups?[i]?.Value;
    }

    public static string InsertEdit(this string input, string insert, int startIndex, int endIndex)
    {
        var before = input[0..startIndex];
        var after = input[endIndex..^0];
        return before + insert + after;
    }

    public static string InsertEdit(this string input, string insert, Range range) =>
        InsertEdit(
            input, insert,
            range.Start.GetOffset(input.Length),
            range.End.GetOffset(input.Length)
        );
}