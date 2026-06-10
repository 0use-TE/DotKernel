namespace DotKernel;

/// <summary>Provides a human-readable description for functions, parameters, or prompts.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class KernelDescriptionAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}
