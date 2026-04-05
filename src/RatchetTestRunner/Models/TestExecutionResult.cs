namespace RatchetTestRunner.Models;

public class TestExecutionResult
{
    public Guid ExecutionId { get; set; } = Guid.NewGuid();
    public TestMethodInfo? TestMethod { get; set; }
    public TestStatus Status { get; set; } = TestStatus.Pending;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration => EndTime - StartTime;
    public List<TestStep> Steps { get; set; } = new();
    public List<BrowserConsoleMessage> BrowserOutput { get; set; } = new();
    public List<ExceptionInfo> Exceptions { get; set; } = new();
    public string? ScreenshotPath { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
}

public enum TestStatus
{
    Pending,
    Running,
    Passed,
    Failed,
    Error,
    Skipped
}
