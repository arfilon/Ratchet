namespace RatchetTestRunner.Models;

public class TestStep
{
    public int Index { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public TimeSpan Duration { get; set; }
    public StepStatus Status { get; set; } = StepStatus.Success;
    public string? ErrorMessage { get; set; }
    public object? Parameters { get; set; }
}

public enum StepStatus
{
    Success,
    Failed,
    Skipped
}
