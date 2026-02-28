using System.Runtime.CompilerServices;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.Tooling;

[InterpolatedStringHandler]
public ref struct ArgumentStringHandlerEx
{
    private DefaultInterpolatedStringHandler _builder;
    private readonly List<string> _secretValues;

    public ArgumentStringHandlerEx(
        int literalLength,
        int formattedCount,
        out bool handlerIsValid)
    {
        _builder = new(literalLength, formattedCount);
        _secretValues = new();
        handlerIsValid = true;
    }

    public static implicit operator ArgumentStringHandlerEx(string value)
    {
        if (value.ContainsAnyOrdinalIgnoreCase("\n", "\r"))
            value = value.AsSingleLine();
        return $"{value}";
    }

    public void AppendLiteral(string value)
    {
        _builder.AppendLiteral(value);
    }

    public void AppendFormatted(object? obj, int alignment = 0, string? format = null)
    {
        switch (obj)
        {
            case string value:
                (value, format) = GetObjectString(value, alignment, format);
                AppendFormatted(value, alignment, format);
            break;
            case IAbsolutePathHolder holder: AppendFormatted(holder, alignment, format); break;
            case ValueTuple<string, object?> optionalParam:
                var (param, arg) = optionalParam;
                if (string.IsNullOrWhiteSpace(arg?.ToString()))
                {
                    return;
                }
                (var stringArg, format) = GetObjectString(arg, alignment, format);
                AppendFormatted(param + stringArg, alignment, format);
            break;
            default: AppendFormatted(obj?.ToString(), alignment, format); break;
        }
    }

    private (string output, string? format) GetObjectString(object? obj, int alignment = 0, string? format = null)
    {
        switch (obj)
        {
            case string value:
                value = value.AsSingleLine();
                switch (format)
                {
                    case "r":
                    case "secret":
                        _secretValues.Add(value);
                    break;

                    case "dn":
                    case "q":
                    case "quote":
                    case "doubleQuote":
                        (value, format) = (value.DoubleQuoteIfNeeded(), null);
                    break;

                    case "sn":
                    case "sq":
                    case "singleQuote":
                        (value, format) = (value.SingleQuoteIfNeeded(), null);
                    break;

                    case "nq": format = null; break;
                }
                return (value, format);

            case IAbsolutePathHolder holder:
                return (holder.Path, format ?? AbsolutePath.DoubleQuoteIfNeeded);

            default: return (obj?.ToString() ?? "", format);
        }
    }

    private void AppendFormatted(string? value, int alignment, string? format)
    {
        _builder.AppendFormatted(value, alignment, format);
    }

    private void AppendFormatted(IAbsolutePathHolder? holder, int alignment, string? format)
    {
        _builder.AppendFormatted(holder?.Path, alignment, format ?? AbsolutePath.DoubleQuoteIfNeeded);
    }

    public string ToStringAndClear()
    {
        var value = _builder.ToStringAndClear().AsSingleLine();

        return value.Length > 1 &&  value.IndexOf(value: '"', startIndex: 1) == value.Length - 1
            ? value.TrimMatchingDoubleQuotes()
            : value;
    }

    public Func<string, string> GetFilter()
    {
        var secretValues = _secretValues;
        return x => secretValues.Aggregate(x, (arguments, value) => arguments.Replace(value, "[REDACTED]"));
    }
}
