namespace DotKernel;

/// <summary>Marks a property as a prompt template variable (fills {{$name}}).</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class PromptVariableAttribute : Attribute
{
    public string? Description { get; set; }

    public string? Default { get; set; }

    public bool Required { get; set; } = true;
}
