using System.Text.RegularExpressions;

namespace DotKernel;

public static class PromptRenderer
{
    private static readonly Regex VariableRegex = new(
        @"\{\{\$([a-zA-Z_][a-zA-Z0-9_]*)\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Render(string template, IReadOnlyDictionary<string, string?> variables)
    {
        return VariableRegex.Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            if (variables.TryGetValue(name, out var value) && value is not null)
            {
                return value;
            }

            return match.Value;
        });
    }

    public static string Render(PromptDefinition prompt, object? instance, IReadOnlyDictionary<string, string?>? extra = null)
    {
        var template = prompt.GetTemplate(instance);
        var variables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var variable in prompt.Variables)
        {
            var value = variable.Getter?.Invoke(instance);
            variables[variable.Name] = value?.ToString() ?? variable.Default;
        }

        if (extra is not null)
        {
            foreach (var pair in extra)
            {
                variables[pair.Key] = pair.Value;
            }
        }

        return Render(template, variables);
    }
}
