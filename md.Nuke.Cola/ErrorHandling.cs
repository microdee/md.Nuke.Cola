using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EverythingSearchClient;

namespace Nuke.Cola;

/// <summary>
/// A record that can represent an attempt at an arbitrary action
/// </summary>
public record class Attempt(Exception[]? Error = null)
{
    public static implicit operator Attempt (Exception[] e) => new(Error: e);
    public static implicit operator Attempt (Exception e) => new(Error: [e]);
    public static implicit operator Exception? (Attempt from) => from.Error?[0];
    public static implicit operator bool (Attempt from) => from.Error == null;
}

/// <summary>
/// A union that can hold either a correct value or an array of errors
/// </summary>
public record class ValueOrError<T>(T? Value = default, Exception[]? Error = null)
{
    public static implicit operator ValueOrError<T> (T val) => new(val);
    public static implicit operator ValueOrError<T> (Exception[]? e) => new(Error: e);
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
            var prevErrors = previousErrors ?? [];
            return prevErrors.Prepend(e).ToArray();
        }
    }

    /// <summary>
    /// Work on the value inside a ValueOrError but only if input ValueOrError is valid. Return aggregated errors
    /// otherwise.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="transform"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="TInput"></typeparam>
    /// <returns></returns>
    public static ValueOrError<TResult> Transform<TResult, TInput>(this ValueOrError<TInput> self, Func<TInput, TResult> transform)
    {
        if (!self) return self.Error;
        return TryGet(() => transform(self.Get()));
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

    /// <summary>
    /// Attempt to try something which may throw an exception. If an exception is thrown then wrap
    /// it inside a ValueOrError for others to handle
    /// </summary>
    /// <param name="action">which may throw an exception</param>
    /// <param name="onFailure">Optionally react to the failure of the action</param>
    /// <param name="previousErrors">Optionally provide previous failures which has led to this one</param>
    /// <returns>An attempt</returns>
    public static Attempt Try(Action action, Action<Exception>? onFailure = null, Exception[]? previousErrors = null)
    {
        try
        {
            action();
            return new();
        }
        catch (Exception e)
        {
            onFailure?.Invoke(e);
            var prevErrors = previousErrors ?? [];
            return prevErrors.Prepend(e).ToArray();
        }
    }

    /// <summary>
    /// If input attempt is an error then attempt to execute another input action
    /// (which may also fail)
    /// </summary>
    /// <param name="self"></param>
    /// <param name="action">which may throw an exception</param>
    /// <param name="onFailure">Optionally react to the failure of the action</param>
    /// <returns>An attempt</returns>
    public static Attempt Else(this Attempt self, Action action, Action<Exception>? onFailure = null)
    {
        if (self) return self;
        return Try(action, onFailure, self.Error);
    }

    /// <summary>
    /// If input attempt is an error then attempt to execute another input action
    /// (which may also fail) only when condition is true.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="condition">Consider fallback only when this condition is true</param>
    /// <param name="action">which may throw an exception</param>
    /// <param name="onFailure">Optionally react to the failure of the action</param>
    /// <returns>
    /// The input correct attempt, or if that has failed before then the attempt at this input
    /// action, or an error if that also fails.
    /// </returns>
    public static Attempt Else(this Attempt self, bool condition, Action action, Action<Exception>? onFailure = null)
    {
        if (self || !condition) return self;
        return Try(action, onFailure, self.Error);
    }

    /// <summary>
    /// Guarantee that one of the chain of attempts proceeding this function has succeeded otherwise
    /// throw the aggregated exceptions inside the error.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="message">Optional message for when input is an error</param>
    public static void Assume(this Attempt self, string? message = null)
    {
        if (self) return;
        if (self.Error!.Length == 1) throw self.Error[0];
        throw message == null
            ? new AggregateException(self.Error!)
            : new AggregateException(message, self.Error!);
    }
}