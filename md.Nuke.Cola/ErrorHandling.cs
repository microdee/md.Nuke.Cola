using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EverythingSearchClient;

namespace Nuke.Cola;

/// <summary>
/// A union that can hold either a correct value or an array of errors
/// </summary>
public record ValueOrError<T>(T? Value = default, Exception[]? Error = null)
{
    public static implicit operator ValueOrError<T> (T val) => new(val);
    public static implicit operator ValueOrError<T> (Exception[] e) => new(Error: e);
    public static implicit operator ValueOrError<T> (Exception e) => new(Error: [e]);
    public static implicit operator T? (ValueOrError<T> from) => from.Value;
    public static implicit operator Exception? (ValueOrError<T> from) => from.Error?[0];
    public static implicit operator bool (ValueOrError<T> from) => from.Error == null;
}

public static class ErrorHandling
{
    /// <summary>
    /// Try to gwt a value from an input function which may throw an exception. If an exception is
    /// thrown then wrap it inside a ValueOrError for others to handle
    /// </summary>
    /// <param name="getter">The function returning T which may however throw an exception</param>
    /// <param name="onFailure">Optionally react to the failure of the getter function</param>
    /// <param name="previousErrors">Optionally provide previous failures which has led to this one</param>
    /// <returns>The result of the getter function or an error</returns>
    public static ValueOrError<T> TryGet<T>(Func<T> getter, Action<Exception>? onFailure = null, Exception[]? previousErrors = null)
    {
        try { return getter(); }
        catch (Exception e)
        {
            onFailure?.Invoke(e);
            var prevErrors = previousErrors ?? Array.Empty<Exception>();
            return prevErrors.Prepend(e).ToArray();
        }
    }

    /// <summary>
    /// If input ValueOrError is an error then attempt to execute the input getter function
    /// (which may also fail)
    /// If input ValueOrError is a value then just return that immediately
    /// </summary>
    /// <param name="self"></param>
    /// <param name="getter">The function returning T which may however throw an exception</param>
    /// <param name="onFailure">Optionally react to the failure of the getter function</param>
    /// <returns>
    /// The input correct value, or if that's erronous then the result of the getter function, or an
    /// error if that also fails.
    /// </returns>
    public static ValueOrError<T> Else<T>(this ValueOrError<T> self, Func<T> getter, Action<Exception>? onFailure = null)
    {
        if (self) return self;
        return TryGet(getter, onFailure, self.Error);
    }

    /// <summary>
    /// If input ValueOrError is an error then attempt to execute the input getter function
    /// (which may also fail) only when condition is true.
    /// If condition is false or when input ValueOrError is a value then just return that immediately
    /// </summary>
    /// <param name="self"></param>
    /// <param name="condition">Consider fallback only when this condition is true</param>
    /// <param name="getter">The function returning T which may however throw an exception</param>
    /// <param name="onFailure">Optionally react to the failure of the getter function</param>
    /// <returns>
    /// The input correct value, or if that's erronous then the result of the getter function, or an
    /// error if that also fails.
    /// </returns>
    public static ValueOrError<T> Else<T>(this ValueOrError<T> self, bool condition, Func<T> getter, Action<Exception>? onFailure = null)
    {
        if (self || !condition) return self;
        return TryGet(getter, onFailure, self.Error);
    }

    /// <summary>
    /// Guarantee the result of an input ValueOrError otherwise throw the aggregated exceptions
    /// inside the error.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="message">Optional message for when input is an error</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>Guaranteed value (or throwing an exception)</returns>
    public static T Get<T>(this ValueOrError<T> self, string? message = null)
    {
        if (self) return self!;
        if (self.Error!.Length == 1) throw self.Error[0];
        throw message == null
            ? new AggregateException(self.Error!)
            : new AggregateException(message, self.Error!);
    }
}