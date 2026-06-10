namespace DotKernel;

/// <summary>Groups prompt properties and prompt variables on a class.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class KernelPromptClassAttribute : Attribute
{
    public string? Name { get; }

    public KernelPromptClassAttribute(string? name = null) => Name = name;
}
