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
}