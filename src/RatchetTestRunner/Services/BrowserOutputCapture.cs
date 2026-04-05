using RatchetTestRunner.Models;

namespace RatchetTestRunner.Services;

public class BrowserOutputCapture : IBrowserOutputCapture
{
    private List<BrowserConsoleMessage> _messages = new();
    public event EventHandler<BrowserConsoleMessage>? OnMessageReceived;

    public void RecordConsoleMessage(ConsoleLevel level, string message)
    {
        var consoleMsg = new BrowserConsoleMessage
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Text = message
        };

        _messages.Add(consoleMsg);
        OnMessageReceived?.Invoke(this, consoleMsg);
    }

    public List<BrowserConsoleMessage> GetCapturedOutput() => _messages.ToList();

    public void ClearOutput()
    {
        _messages.Clear();
    }
}
