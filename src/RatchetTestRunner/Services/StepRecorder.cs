using System.Diagnostics;
using RatchetTestRunner.Models;

namespace RatchetTestRunner.Services;

public class StepRecorder
{
    private List<TestStep> _steps = new();
    private int _stepIndex = 0;

    public void RecordStep(
        string action,
        TimeSpan duration,
        bool success = true,
        string? details = null,
        object? parameters = null,
        Exception? exception = null)
    {
        var step = new TestStep
        {
            Index = _stepIndex++,
            Timestamp = DateTime.UtcNow,
            Action = action,
            Details = details,
            Duration = duration,
            Status = success ? StepStatus.Success : StepStatus.Failed,
            Parameters = parameters,
            ErrorMessage = exception?.Message
        };

        _steps.Add(step);
    }

    public void RecordStepWithStackTrace(
        string methodName,
        TimeSpan duration,
        bool success = true,
        object? parameters = null,
        Exception? exception = null)
    {
        var stackTrace = new StackTrace(true);
        var frames = stackTrace.GetFrames() ?? Array.Empty<StackFrame>();

        // Try to find context from the call stack
        var details = ExtractContextFromStackTrace(frames);

        RecordStep(methodName, duration, success, details, parameters, exception);
    }

    private string? ExtractContextFromStackTrace(StackFrame[] frames)
    {
        // Look for the first frame that's not in this service
        foreach (var frame in frames.Skip(1))
        {
            var method = frame.GetMethod();
            if (method?.DeclaringType?.Name != nameof(StepRecorder))
            {
                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();
                if (!string.IsNullOrEmpty(fileName))
                {
                    return $"{Path.GetFileName(fileName)}:{lineNumber} in {method?.Name}";
                }
            }
        }

        return null;
    }

    public List<TestStep> GetRecordedSteps() => _steps.ToList();

    public void ClearSteps()
    {
        _steps.Clear();
        _stepIndex = 0;
    }
}
