using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.Tooling;

public class XRepoItem : IEnumerable<XRepoItem>
{
    public enum Kind
    {
        Root,
        Package,
        Key,
        Property,
        Value,
        Invalid
    }

    public required Kind ItemKind { init; get; }
    public string? Value { init; get; }
    public string? Key { init; get; }

    private List<XRepoItem> _unnamedItems = [];
    private Dictionary<string, XRepoItem> _namedItems = [];

    public IEnumerator<XRepoItem> GetEnumerator()
    {
        foreach (var item in _unnamedItems) yield return item;
        foreach (var item in _namedItems.Values) yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (var item in _unnamedItems) yield return item;
        foreach (var item in _namedItems.Values) yield return item;
    }

    public XRepoItem? this[int i] => i >= 0 && i < _unnamedItems.Count ? _unnamedItems[i] : null;
    public XRepoItem? this[string i] => _namedItems.TryGetValue(i, out var output) ? output : null;

    private const int MinimumIndent = 4;

    private static bool IsItemLine(string line)
        => !string.IsNullOrWhiteSpace(line)
            && line.StartsWith(new string(' ', MinimumIndent))
            && line.TrimStart().StartsWithAny("require", "->");

    private static bool IsWithinCurrentItem(string line, int indent)
        => !IsItemLine(line) || GetIndent(line) > indent;

    private static int GetIndent(string line) => line.TakeWhile(c => c == ' ').Count();

    public static XRepoItem Parse(ref List<string> infoOutput, ref int i)
    {
        var line = infoOutput[i];
        int indent = GetIndent(line);
        var options = RegexOptions.IgnoreCase;
        // TODO: compile regex patterns in compile time
        var packKey = line.Parse(@"\srequire\((?<KEY>[a-z].*)\)\:", options)("KEY");
        var keyOnly = line.Parse(@"\s->\s(?<KEY>[a-z]\w*)\:$", options)("KEY");
        var propWithValue = line.Parse(@"\s->\s(?:(?<KEY>[a-z]\w*)\:\s)?(?<VALUE>.+)$", options);

        Kind kind = packKey != null                                          ? Kind.Package
            : keyOnly != null                                                ? Kind.Key
            : propWithValue("KEY") != null && propWithValue("VALUE") != null ? Kind.Property
            : propWithValue("VALUE") != null                                 ? Kind.Value
            : Kind.Invalid;

        var key = kind switch {
            Kind.Package  => packKey,
            Kind.Key      => keyOnly,
            Kind.Property => propWithValue("KEY"),
            _ => null
        };

        var value = kind switch {
            Kind.Property or Kind.Value => propWithValue("VALUE"),
            _ => null
        };

        var result = new XRepoItem { ItemKind = kind, Key = key, Value = value };

        i++;
        while(i < infoOutput.Count && IsWithinCurrentItem(line, indent))
        {
            line = infoOutput[i];
            if (!IsItemLine(line))
            {
                i++;
                continue;
            }
            var item = Parse(ref infoOutput, ref i);
            if (item.Key == null)
                result._unnamedItems.Add(item);
            else
                result._namedItems.Add(item.Key!, item);
        }

        return result;
    }

    public static XRepoItem Parse(IReadOnlyCollection<Output> toolOutput)
    {
        var infoOutput = toolOutput
            .Where(o => o.Type == OutputType.Std)
            .Select(o => o.Text)
            .ToList();
        var result = new XRepoItem { ItemKind = Kind.Root };

        int i = 0;
        string line = "";
        while(i < infoOutput.Count && IsWithinCurrentItem(line, MinimumIndent))
        {
            line = infoOutput[i];
            if (!IsItemLine(line))
            {
                i++;
                continue;
            }
            var item = Parse(ref infoOutput, ref i);
            if (item.Key == null)
                result._unnamedItems.Add(item);
            else
                result._namedItems.Add(item.Key!, item);
        }
        return result;
    }
}