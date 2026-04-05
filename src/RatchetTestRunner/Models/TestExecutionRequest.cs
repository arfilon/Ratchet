namespace RatchetTestRunner.Models;

public class TestExecutionRequest
{
    public TestMethodInfo? TestMethod { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public int TimeoutMs { get; set; } = 120000;
    public bool CaptureScreenshot { get; set; } = true;
    public string? CustomContext { get; set; }
}
