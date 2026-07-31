using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace DotKernel;

internal static class ArgumentConverter
{
    public static T ConvertTo<T>(object? value, string argumentName, string functionName)
    {
        if (value is null)
        {
            if (default(T) is null)
            {
                return default!;
            }

            throw new KernelException($"Argument '{argumentName}' for '{functionName}' is null.");
        }

        if (value is T typed)
        {
            return typed;
        }

        var target = typeof(T);
        var underlying = Nullable.GetUnderlyingType(target) ?? target;

        if (value is JsonElement json)
        {
            return ConvertJsonElement<T>(json, underlying, argumentName, functionName);
        }

        if (underlying.IsEnum)
        {
            if (value is string enumName)
            {
                return (T)Enum.Parse(underlying, enumName, ignoreCase: true);
            }

            return (T)Enum.ToObject(underlying, System.Convert.ChangeType(value, Enum.GetUnderlyingType(underlying), CultureInfo.InvariantCulture)!);
        }

        if (underlying == typeof(Guid) && value is string guidText)
        {
            return (T)(object)Guid.Parse(guidText);
        }

        if (underlying == typeof(DateTime) && value is string dtText)
        {
            return (T)(object)DateTime.Parse(dtText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (underlying == typeof(DateTimeOffset) && value is string dtoText)
        {
            return (T)(object)DateTimeOffset.Parse(dtoText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (value is string s && underlying == typeof(string))
        {
            return (T)(object)s;
        }

        try
        {
            return (T)System.Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new KernelException(
                $"Cannot convert argument '{argumentName}' for '{functionName}' to {typeof(T).Name}: {ex.Message}");
        }
    }

    private static T ConvertJsonElement<T>(JsonElement json, Type underlying, string argumentName, string functionName)
    {
        if (json.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            if (default(T) is null)
            {
                return default!;
            }

            throw new KernelException($"Argument '{argumentName}' for '{functionName}' is null.");
        }

        if (underlying == typeof(string))
        {
            return (T)(object)(json.ValueKind == JsonValueKind.String
                ? json.GetString() ?? string.Empty
                : json.GetRawText());
        }

        if (underlying == typeof(bool))
        {
            return (T)(object)json.GetBoolean();
        }

        if (underlying == typeof(int))
        {
            return (T)(object)json.GetInt32();
        }

        if (underlying == typeof(long))
        {
            return (T)(object)json.GetInt64();
        }

        if (underlying == typeof(float))
        {
            return (T)(object)json.GetSingle();
        }

        if (underlying == typeof(double))
        {
            return (T)(object)json.GetDouble();
        }

        if (underlying == typeof(decimal))
        {
            return (T)(object)json.GetDecimal();
        }

        if (underlying == typeof(Guid))
        {
            return (T)(object)json.GetGuid();
        }

        if (underlying == typeof(DateTime))
        {
            return (T)(object)json.GetDateTime();
        }

        if (underlying == typeof(DateTimeOffset))
        {
            return (T)(object)json.GetDateTimeOffset();
        }

        if (underlying.IsEnum)
        {
            if (json.ValueKind == JsonValueKind.String)
            {
                return (T)Enum.Parse(underlying, json.GetString()!, ignoreCase: true);
            }

            if (json.ValueKind == JsonValueKind.Number)
            {
                return (T)Enum.ToObject(underlying, json.GetInt64());
            }
        }

        if (underlying == typeof(JsonElement))
        {
            return (T)(object)json.Clone();
        }

        try
        {
            return DeserializeComplex<T>(json, underlying);
        }
        catch (Exception ex)
        {
            throw new KernelException(
                $"Cannot deserialize argument '{argumentName}' for '{functionName}' to {typeof(T).Name}: {ex.Message}");
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Complex argument binding is best-effort; AOT apps should prefer primitive parameters.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Complex argument binding is best-effort; AOT apps should prefer primitive parameters.")]
    private static T DeserializeComplex<T>(JsonElement json, Type underlying)
    {
        var deserialized = json.Deserialize(underlying);
        if (deserialized is T result)
        {
            return result;
        }

        return (T)deserialized!;
    }
}
