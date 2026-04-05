namespace RatchetTestRunner.Models;

public class TestExecutionProgress
{
    public TestExecutionState State { get; set; } = TestExecutionState.Starting;
    public TestStep? CurrentStep { get; set; }
    public BrowserConsoleMessage? ConsoleMessage { get; set; }
    public ExceptionInfo? Exception { get; set; }
    public int PercentComplete { get; set; }
    public string? Message { get; set; }
}

public enum TestExecutionState
{
    Starting,
    Initializing,
    Running,
    Capturing,
    Cleaning,
    Complete
}
