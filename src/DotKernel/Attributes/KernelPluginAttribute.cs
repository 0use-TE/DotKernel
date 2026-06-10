namespace DotKernel;

/// <summary>Marks a class as a kernel plugin container.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class KernelPluginAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
