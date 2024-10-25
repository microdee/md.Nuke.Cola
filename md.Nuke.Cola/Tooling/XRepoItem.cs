using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Cola.Tooling;

/// <summary>
/// A structured representation of the data `xrepo info` prints out about a package.
/// `xrepo info` uses a bespoke format which expresses data relationship with indentation. One item
/// can have multiple items associated with it if they're indented further inside. It's almost like
/// YAML but it differs ever so slightly. Items can have a key optionally if they have : after a
/// single first word these are named items and accessible with a string indexer. Value only items
/// are accessible only through an integer indexer. All items are iterated upon via the IEnumerable
/// interface.
/// </summary>
public class XRepoItem : IEnumerable<XRepoItem>
{
    /// <summary>
    /// Kind of the info item represented
    /// </summary>
    public enum Kind
    {
        /// <summary>
        /// The main item containing all other items output by `xrepo info`.
        /// It has no key and no value
        /// </summary>
        Root,

        /// <summary>
        /// Represents data abpuut a package (require(myPackage)). It has Key only.
        /// </summary>
        Package,

        /// <summary>
        /// An item which has only a key and no value ( -> mykey:)
        /// </summary>
        Key,

        /// <summary>
        /// An item which has both a key and a value ( -> mykey: foobar)
        /// </summary>
        Property,

        /// <summary>
        /// An item which doesn't have a key associated usually a list of stuff ( -> foobar)
        /// </summary>
        Value,

        /// <summary>
        /// If for any cursed reason an item couldn't be parsed while it was passing the IsItemLine
        /// check. This indicates a bug in the code.
        /// </summary>
        Invalid
    }

    /// <summary>
    /// To help distinguish items from each other
    /// </summary>
    public required Kind ItemKind { init; get; }

    /// <summary>
    /// This item specifies a value which is arbitrary text either after `key:` or the entire line if
    /// key was not present.
    /// </summary>
    public string? Value { init; get; }

    /// <summary>
    /// This item specifies a key which is a short identifier at the beginning of the line like `mykey:`
    /// </summary>
    /// <value></value>
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

    /// <summary>
    /// Get one unnamed sub-item, return null if out-of-bounds
    /// </summary>
    public XRepoItem? this[int i] => i >= 0 && i < _unnamedItems.Count ? _unnamedItems[i] : null;

    /// <summary>
    /// Get one named sub-item, return null if doesn't exist
    /// </summary>
    public XRepoItem? this[string i] => _namedItems.TryGetValue(i, out var output) ? output : null;

    private const int MinimumIndent = 4;

    private static bool IsItemLine(string line)
        => !string.IsNullOrWhiteSpace(line)
            && line.StartsWith(new string(' ', MinimumIndent))
            && line.TrimStart().StartsWithAny("require", "->");

    private static bool IsWithinCurrentItem(string line, int indent)
        => !IsItemLine(line) || GetIndent(line) > indent;

    private static int GetIndent(string line) => line.TakeWhile(c => c == ' ').Count();

    private static XRepoItem Parse(ref List<string> infoOutput, ref int i)
    {
        var line = infoOutput[i];
        int indent = GetIndent(line);
        var options = RegexOptions.IgnoreCase;
        // TODO: compile regex patterns in compile time
        var packKey = line.Parse(@"\srequire\((?<KEY>[a-z].*)\)\:", options, forceNullOnWhitespce: true)("KEY");
        var keyOnly = line.Parse(@"\s->\s(?<KEY>[a-z]\w*)\:$", options, forceNullOnWhitespce: true)("KEY");
        var propWithValue = line.Parse(@"\s->\s(?:(?<KEY>[a-z]\w*)\:\s)?(?<VALUE>.+)$", options, forceNullOnWhitespce: true);

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
        while(i < infoOutput.Count)
        {
            line = infoOutput[i];
            if (!IsWithinCurrentItem(line, indent)) break;
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

    /// <summary>
    /// Parse the xrepo info structure from a Tool output
    /// </summary>
    internal static XRepoItem Parse(IEnumerable<Output> toolOutput)
    {
        var infoOutput = toolOutput
            .Where(o => o.Type == OutputType.Std)
            .Select(o => o.Text.TrimEnd())
            .ToList();
        var result = new XRepoItem { ItemKind = Kind.Root };
        int i = infoOutput.FindIndex(0, IsItemLine);
        
        string line = "";
        while(i < infoOutput.Count)
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