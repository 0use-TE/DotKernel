namespace DotKernel;

/// <summary>
/// Marks a property as live context that is injected into the model request
/// (name + description + current value). Does not become a tool.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class KernelPropertyAttribute(string name, string? description = null) : Attribute
{
    /// <summary>Context key exposed to the model (e.g. "current_speed").</summary>
    public string Name { get; } = name;

    /// <summary>What this property means.</summary>
    public string? Description { get; } = description;
}
