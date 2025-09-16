using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace Nuke.Cola;

public static class Cola
{
    /// <summary>
    /// Syntax simplifier when `new()` cannot be used for dictionaries
    /// </summary>
    public static Dictionary<Key, Value> MakeDictionary<Key, Value>(params (Key key, Value value)[] items)
        where Key : notnull
        => items.ToDictionary(i => i.key, i => i.value);

    /// <summary>
    /// Merge two dictionaries safely
    /// </summary>
    public static IReadOnlyDictionary<Key, Value>? Merge<Key, Value>(
        this IReadOnlyDictionary<Key, Value>? a,
        IReadOnlyDictionary<Key, Value>? b
    ) where Key : notnull
    {
        if (a == null) return b;
        if (b == null) return a;
        
        var result = a.ToDictionary();
        foreach (var i in b)
        {
            result[i.Key] = i.Value;
        }

        return result;
    }
    
    /// <summary>
    /// Merge two dictionaries safely
    /// </summary>
    public static IDictionary<Key, Value>? MergeMutable<Key, Value>(
        this IDictionary<Key, Value>? a,
        IDictionary<Key, Value>? b
    ) where Key : notnull
    {
        if (a == null) return b;
        if (b == null) return a;
        
        var result = a.ToDictionary();
        foreach (var i in b)
        {
            result[i.Key] = i.Value;
        }

        return result;
    }

    /// <summary>
    /// Simply invoke a task on an object and return that same object
    /// </summary>
    public static T With<T>(this T self, Action<T> operation)
    {
        operation(self);
        return self;
    }

    /// <summary>
    /// Simply invoke a task on an object which may be null and return that same object
    /// </summary>
    public static T? WithNullable<T>(this T? self, Action<T>? operation)
    {
        if (self != null)
            operation?.Invoke(self);
        return self;
    }
}