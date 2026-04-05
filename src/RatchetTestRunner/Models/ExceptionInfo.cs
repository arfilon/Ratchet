namespace RatchetTestRunner.Models;

public class ExceptionInfo
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public ExceptionInfo? InnerException { get; set; }
    public int AssociatedStepIndex { get; set; } = -1;
    public string Context { get; set; } = string.Empty;
}
