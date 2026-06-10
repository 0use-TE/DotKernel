using System.Text.Json;
using Microsoft.Extensions.AI;

namespace DotKernel;

internal sealed class KernelAIFunction : AIFunction
{
    private readonly KernelFunctionDescriptor _descriptor;
    private readonly JsonElement _schema;

    public KernelAIFunction(KernelFunctionDescriptor descriptor)
    {
        _descriptor = descriptor;
        using var document = JsonDocument.Parse(descriptor.ParametersSchemaJson);
        _schema = document.RootElement.Clone();
    }

    public override string Name => _descriptor.ToolName;

    public override string Description => _descriptor.Description ?? string.Empty;

    public override JsonElement JsonSchema => _schema;

    protected override ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var pair in arguments)
        {
            dict[pair.Key] = pair.Value;
        }

        var context = new FunctionInvocationContext
        {
            Descriptor = _descriptor,
            Arguments = dict,
        };

        return _descriptor.Invoker(context, cancellationToken);
    }
}
