namespace DotKernel;

/// <summary>Marks a property as a prompt template.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class KernelPromptAttribute(string name) : Attribute
{
    public string Name { get; } = name;

    public string? Description { get; set; }

    public PromptRole Role { get; set; } = PromptRole.User;
}
