using System.Text;
using System.Text.Json;

namespace DotKernel;

internal static class ToolCallArgumentFormatter
{
    public static string? FormatRawJson(object? arguments)
    {
        if (arguments is null)
        {
            return null;
        }

        if (arguments is JsonElement json)
        {
            return json.GetRawText();
        }

        if (arguments is IReadOnlyDictionary<string, object?> readOnly)
        {
            return FormatDictionary(readOnly);
        }

        if (arguments is IDictionary<string, object?> dict)
        {
            return FormatDictionary(dict);
        }

        return arguments.ToString();
    }

    private static string FormatDictionary(IEnumerable<KeyValuePair<string, object?>> arguments)
    {
        var sb = new StringBuilder();
        sb.Append('{');

        var first = true;
        foreach (var (key, value) in arguments)
        {
            if (!first)
            {
                sb.Append(',');
            }

            first = false;
            sb.Append('"').Append(Escape(key)).Append("\":");
            sb.Append(FormatValue(value));
        }

        sb.Append('}');
        return sb.ToString();
    }

    private static string FormatValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{Escape(s)}\"",
        bool b => b ? "true" : "false",
        JsonElement json => json.GetRawText(),
        _ => $"\"{Escape(value.ToString() ?? string.Empty)}\"",
    };

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
