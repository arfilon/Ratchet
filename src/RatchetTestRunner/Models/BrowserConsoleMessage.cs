namespace RatchetTestRunner.Models;

public class BrowserConsoleMessage
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public ConsoleLevel Level { get; set; } = ConsoleLevel.Log;
    public string Text { get; set; } = string.Empty;
}

public enum ConsoleLevel
{
    Log,
    Info,
    Warning,
    Error
}
