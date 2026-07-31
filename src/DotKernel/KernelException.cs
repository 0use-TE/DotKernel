namespace DotKernel;

public class KernelException(string message) : Exception(message);

public sealed class ToolCallCancelledException(string toolName)
    : KernelException($"Tool call '{toolName}' was cancelled by a filter.");

public sealed class MaxToolCallRoundsExceededException(int maxRounds)
    : KernelException($"Exceeded maximum tool-call rounds ({maxRounds}).");
