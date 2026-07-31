using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace DotKernel;

internal static class ToolResultFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    public static string Format(object? value)
    {
        switch (value)
        {
            case null:
                return string.Empty;
            case string s:
                return s;
            case bool b:
                return b ? "true" : "false";
            case JsonElement je:
                return je.ValueKind == JsonValueKind.String
                    ? je.GetString() ?? string.Empty
                    : je.GetRawText();
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            case IFormattable formattable when value.GetType().IsEnum:
                return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
            default:
                return FormatComplex(value);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Best-effort JSON for complex tool results; primitives are handled above.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Best-effort JSON for complex tool results; prefer returning string from AOT plugins.")]
    private static string FormatComplex(object value)
    {
        try
        {
            return JsonSerializer.Serialize(value, value.GetType(), JsonOptions);
        }
        catch
        {
            return value.ToString() ?? string.Empty;
        }
    }
}
