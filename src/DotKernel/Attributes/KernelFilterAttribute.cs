namespace DotKernel;

/// <summary>Marks a filter class for automatic registration via source generator.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class KernelFilterAttribute : Attribute
{
    public int Priority { get; set; }
}
