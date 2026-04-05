using RatchetTestRunner.Models;

namespace RatchetTestRunner.Services;

public class ExceptionTracker : IExceptionTracker
{
    private List<ExceptionInfo> _exceptions = new();
    public event EventHandler<ExceptionInfo>? OnExceptionRecorded;

    public void RecordException(Exception ex, string context, int? associatedStepIndex = null)
    {
        var exceptionInfo = new ExceptionInfo
        {
            Timestamp = DateTime.UtcNow,
            ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace ?? string.Empty,
            Context = context,
            AssociatedStepIndex = associatedStepIndex ?? -1
        };

        // Handle inner exceptions
        if (ex.InnerException != null)
        {
            exceptionInfo.InnerException = ConvertException(ex.InnerException);
        }

        _exceptions.Add(exceptionInfo);
        OnExceptionRecorded?.Invoke(this, exceptionInfo);
    }

    public List<ExceptionInfo> GetRecordedExceptions() => _exceptions.ToList();

    public void ClearExceptions()
    {
        _exceptions.Clear();
    }

    private ExceptionInfo ConvertException(Exception ex)
    {
        return new ExceptionInfo
        {
            Timestamp = DateTime.UtcNow,
            ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace ?? string.Empty,
            InnerException = ex.InnerException != null ? ConvertException(ex.InnerException) : null
        };
    }
}
