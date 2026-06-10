namespace DotKernel.Tests;

public class PromptRendererTests
{
    [Fact]
    public void Render_replaces_variables()
    {
        const string template = "Hello {{$name}}, today is {{$day}}.";
        var result = PromptRenderer.Render(template, new Dictionary<string, string?>
        {
            ["name"] = "Alice",
            ["day"] = "Monday",
        });

        Assert.Equal("Hello Alice, today is Monday.", result);
    }
}
