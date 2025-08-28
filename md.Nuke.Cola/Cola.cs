using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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