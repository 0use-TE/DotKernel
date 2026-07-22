# Plugins & Prompts

## Plugin class

Mark a **partial** class with `[KernelPlugin("name")]` and expose members with attributes. The source generator emits `Register()`.

| Target | Attribute | Notes |
|--------|-----------|-------|
| Method | `[KernelFunction("id")]` | Becomes an AI tool (async OK) |
| Property | `[KernelPrompt("id")]` | **Prompt template** (string may contain `{{$var}}`) |
| Property | `[PromptVariable]` | **Template argument** — fills `{{$PropertyName}}` in the same type's prompts |
| Property | `[KernelProperty("name", "description")]` | Live context each turn (unrelated to prompt templates) |

## Live context — `[KernelProperty]`

Marked properties are **not** tools. On every model call, DotKernel reads their current values and prepends a temporary system message (not stored in `ChatHistory`):

```csharp
[KernelPlugin("Twin")]
public partial class BuildingTwinPlugin(BuildingTwinState state)
{
    [KernelProperty("agv_zone", "Zone where the AGV currently is")]
    public string AgvZone => state.AgvZoneId;

    [KernelProperty("total_actions", "Twin mutations this session")]
    public int TotalActions => state.TotalActions;
}
```

Example injected text:

```text
## Live context
- Twin.agv_zone (Zone where the AGV currently is): storage
- Twin.total_actions (Twin mutations this session): 3
```

Helpers: `kernel.GetPropertyContext()`, `kernel.RenderPropertyContext()`.

## Prompt templates — `[KernelPrompt]` and `[PromptVariable]`

Use these together to **render reusable prompt strings**. They are **not** AI tools.

| Attribute | Role |
|-----------|------|
| `[KernelPrompt("id")]` | On a string property: the property value is the template body; `"id"` is the template name |
| `[PromptVariable]` | On other properties of the same type: their values substitute `{{$PropertyName}}` in the template |

Placeholder syntax: `{{$Name}}` (`$` + property name, case-insensitive).

### Fixed prompt (no variables)

Only `[KernelPrompt]` is required:

```csharp
[KernelPlugin("assistant")]
public partial class AssistantPlugin
{
    [KernelPrompt("system", Role = PromptRole.System)]
    public string SystemInstructions =>
        "Reply in Markdown. Be concise.";
}
```

Optional `Role`: `System` / `User` / `Assistant`.

### Templates with variables (`[KernelPromptClass]`)

Use a dedicated `partial` class marked `[KernelPromptClass]`:

```csharp
[KernelPromptClass("Email")]
public partial class EmailPrompt
{
    // Arguments → {{$Recipient}}, {{$Tone}}
    [PromptVariable(Description = "Recipient")]
    public string Recipient { get; set; } = "";

    [PromptVariable(Description = "Tone", Default = "formal")]
    public string Tone { get; set; } = "formal";

    // Template named "draft"
    [KernelPrompt("draft")]
    public string DraftTemplate =>
        "Write a {{$Tone}} email to {{$Recipient}}: {{$input}}";
}
```

Render:

```csharp
var email = new EmailPrompt { Recipient = "Alex", Tone = "brief" };

// Extra placeholders (no matching property) come from the dictionary, e.g. {{$input}}
var text = kernel.RenderPrompt<EmailPrompt>("draft", new Dictionary<string, string?>
{
    ["input"] = "Request a one-week extension",
}, email);

// text == "Write a brief email to Alex: Request a one-week extension"
```

Substitution order:

1. All `[PromptVariable]` properties on the instance (honoring `Default`)
2. Then the dictionary passed to `RenderPrompt` (override / add, e.g. `input`)
3. Unresolved `{{$xxx}}` placeholders are left unchanged

Full name also works: `kernel.RenderPrompt("Email.draft", variables, instance)`.

**vs `[KernelProperty]`:** `PromptVariable` only fills a template when you call `RenderPrompt`; `KernelProperty` is live state injected automatically on every `InvokeAsync`.

## Streaming

```csharp
await foreach (var update in kernel.InvokeStreamingAsync(input, history, cancellationToken))
{
    switch (update.Kind)
    {
        case KernelStreamingUpdateKind.TextDelta:
            Console.Write(update.TextDelta);
            break;
        case KernelStreamingUpdateKind.ToolCallCompleted:
            // Log update.ToolName / ToolResult separately if needed
            break;
        case KernelStreamingUpdateKind.Completed:
            break;
    }
}
```

## Source generator (AOT)

Reference `DotKernel` (generator ships in the package). Use `[KernelPlugin]` on **partial** classes — registration is emitted at compile time for trimming-friendly builds.
