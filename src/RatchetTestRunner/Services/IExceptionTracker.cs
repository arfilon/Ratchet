using RatchetTestRunner.Models;

namespace RatchetTestRunner.Services;

public interface IExceptionTracker
{
    void RecordException(Exception ex, string context, int? associatedStepIndex = null);
    List<ExceptionInfo> GetRecordedExceptions();
    void ClearExceptions();
    event EventHandler<ExceptionInfo>? OnExceptionRecorded;
}
