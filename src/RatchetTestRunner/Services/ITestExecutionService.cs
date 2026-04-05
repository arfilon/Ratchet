using RatchetTestRunner.Models;

namespace RatchetTestRunner.Services;

public interface ITestExecutionService
{
    Task<TestExecutionResult> ExecuteTestAsync(
        TestMethodInfo testMethod,
        TestExecutionRequest request,
        IProgress<TestExecutionProgress>? progress = null);

    Task<List<TestExecutionResult>> ExecuteTestSequenceAsync(
        List<TestMethodInfo> testMethods,
        IProgress<TestExecutionProgress>? progress = null);

    void CancelExecution();
}
