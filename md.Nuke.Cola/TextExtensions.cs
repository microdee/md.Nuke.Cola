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
    public static IEnumerable<string> DoubleQuoteIfNeeded(this IEnumerable<object> self)
        => self?.Select(s => s.ToString().DoubleQuoteIfNeeded()) ?? Enumerable.Empty<string>();

    /// <summary>
    /// Shorthand for one-liner regex parsing from named captures.
    /// </summary>
    /// <example>
    /// "Hello World".Parse(@"Hello (?&lt;SUBJECT&gt;\w+)")("SUBJECT")
    /// </example>
    /// <returns>A function to be called with the desired capture name</returns>
    public static Func<string, string?> Parse(this string? input, string pattern)
        => Parse(input, new Regex(pattern));

    /// <summary>
    /// Shorthand for one-liner regex parsing from named captures. Allows to use precompiled pattern.
    /// </summary>
    /// <example>
    /// "Hello World".Parse(new Regex(@"Hello (?&lt;SUBJECT&gt;\w+)"))("SUBJECT")
    /// </example>
    /// <returns>A function to be called with the desired capture name</returns>
    public static Func<string, string?> Parse(this string? input, Regex pattern)
    {
        if (input == null) return i => null;
        
        var groups = pattern.Matches(input)?.FirstOrDefault()?.Groups;
        return i => groups?[i]?.Value;
    }
}