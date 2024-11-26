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
    public static Func<string, string?> Parse(
        this string? input,
        string pattern,
        RegexOptions options = RegexOptions.None,
        bool forceNullOnEmpty = false,
        bool forceNullOnWhitespce = false
    ) => Parse(input, new Regex(pattern, options), forceNullOnEmpty, forceNullOnWhitespce);

    /// <summary>
    /// Shorthand for one-liner regex parsing from named captures. Allows to use precompiled pattern.
    /// </summary>
    /// <example>
    /// "Hello World".Parse(new Regex(@"Hello (?&lt;SUBJECT&gt;\w+)"))("SUBJECT")
    /// </example>
    /// <returns>A function to be called with the desired capture name</returns>
    public static Func<string, string?> Parse(
        this string? input,
        Regex pattern,
        bool forceNullOnEmpty = false,
        bool forceNullOnWhitespce = false
    ) {
        if (input == null) return i => null;
        
        var groups = pattern.Matches(input)?.FirstOrDefault()?.Groups;
        return i => forceNullOnEmpty || forceNullOnWhitespce
            ? groups?[i]?.Value.Else(ignoreWhitespace: forceNullOnWhitespce)
            : groups?[i]?.Value;
    }

    /// <summary>
    /// Simple convenience function for replacing new lines with other string (space by default)
    /// </summary>
    public static string AsSingleLine(this string input, string replaceWith = " ")
        => input.Replace(Environment.NewLine, replaceWith).Replace("\n", replaceWith);

    /// <summary>
    /// Converts a glob expression into a Regex expression with captures
    /// </summary>
    /// <param name="glob"></param>
    /// <returns></returns>
    public static string GlobToRegex(this string glob) => Regex.Escape(glob)
        // Escape /
        .Replace("/", @"\/")
        // preprocess wildcards directly after backslash (edge case)
        .Replace(@"\*", "*")
        // Singular * wildcard is converted to capturing ([^\/]*) pattern with surroundings (match everything within current level)
        .ReplaceRegex(@"(?<PRE>[^\*]|^)\*(?<POST>[^\*])", m => $@"{m.Groups["PRE"]}([^\/]*){m.Groups["POST"]}")
        // Recursive ** wildcard at the beginning of the expression is simply converted to capturing (.*) pattern
        .ReplaceRegex(@"^\*\*", m => @"^(.*)")
        // Recursive ** wildcard at the beginning of path components converted into capturing [\\\/]?(.*) pattern
        .ReplaceRegex(@"(?:\\\\|\\\/)\*\*", m => @"[\\\/]?(.*)");

    /// <summary>
    /// Convenience, append a piece of string to an input string only if input string is non-null and non-empty
    /// </summary>
    public static string AppendNonEmpty(this string? self, string other)
        => string.IsNullOrWhiteSpace(self) ? "" : self + other;

    /// <summary>
    /// Convenience, prepend a piece of string to an input string only if input string is non-null and non-empty
    /// </summary>
    public static string PrependNonEmpty(this string? self, string other)
        => string.IsNullOrWhiteSpace(self) ? "" : other + self;

    /// <summary>
    /// Defer to a default value if string is null or empty or whitespace only
    /// </summary>
    /// <param name="self"></param>
    /// <param name="def">
    /// Default value to substitute null/empty/whitespace with (null by default)
    /// </param>
    /// <param name="ignoreWhitespace">
    /// If set to true treat whitespace-only string as empty as well. (true by default)
    /// </param>
    /// <returns></returns>
    public static string? Else(this string? self, string? def = null, bool ignoreWhitespace = true)
        => (string.IsNullOrWhiteSpace(self) && ignoreWhitespace) || string.IsNullOrEmpty(self)
            ? def : self;
}