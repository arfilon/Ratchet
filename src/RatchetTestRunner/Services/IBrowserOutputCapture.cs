using RatchetTestRunner.Models;

namespace RatchetTestRunner.Services;

public interface IBrowserOutputCapture
{
    void RecordConsoleMessage(ConsoleLevel level, string message);
    List<BrowserConsoleMessage> GetCapturedOutput();
    void ClearOutput();
    event EventHandler<BrowserConsoleMessage>? OnMessageReceived;
}
