namespace DotKernel.AvaExample.Twin;

public sealed class TwinActivityEntry(string category, string message, DateTime timestamp)
{
    public string Category { get; } = category;

    public string Message { get; } = message;

    public DateTime Timestamp { get; } = timestamp;

    public string TimeLabel => Timestamp.ToString("HH:mm:ss");
}
