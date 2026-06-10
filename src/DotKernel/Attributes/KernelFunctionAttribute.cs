namespace DotKernel;

/// <summary>Marks a method as an invokable kernel function (AI tool).</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class KernelFunctionAttribute : Attribute
{
    public string? Name { get; }

    public KernelFunctionAttribute(string? name = null) => Name = name;
}
