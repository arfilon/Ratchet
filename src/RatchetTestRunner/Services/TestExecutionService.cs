using System.Diagnostics;
using System.Reflection;
using RatchetTestRunner.Models;

namespace RatchetTestRunner.Services;

public class TestExecutionService : ITestExecutionService
{
    private readonly ILogger<TestExecutionService> _logger;
    private readonly IExceptionTracker _exceptionTracker;
    private readonly IBrowserOutputCapture _outputCapture;
    private CancellationTokenSource? _cancellationTokenSource;

    public TestExecutionService(
        ILogger<TestExecutionService> logger,
        IExceptionTracker exceptionTracker,
        IBrowserOutputCapture outputCapture)
    {
        _logger = logger;
        _exceptionTracker = exceptionTracker;
        _outputCapture = outputCapture;
    }

    public async Task<TestExecutionResult> ExecuteTestAsync(
        TestMethodInfo testMethod,
        TestExecutionRequest request,
        IProgress<TestExecutionProgress>? progress = null)
    {
        var result = new TestExecutionResult
        {
            TestMethod = testMethod,
            ExecutionId = Guid.NewGuid(),
            Status = TestStatus.Running,
            StartTime = DateTime.UtcNow
        };

        _cancellationTokenSource = new CancellationTokenSource();
        var timeout = TimeSpan.FromMilliseconds(request.TimeoutMs);
        _cancellationTokenSource.CancelAfter(timeout);

        try
        {
            // Clear previous data
            _exceptionTracker.ClearExceptions();
            _outputCapture.ClearOutput();

            progress?.Report(new TestExecutionProgress
            {
                State = TestExecutionState.Starting,
                PercentComplete = 0,
                Message = $"Starting test: {testMethod.DisplayName}"
            });

            if (testMethod.ParentClass?.Type == null)
            {
                throw new InvalidOperationException("Test class type is not available");
            }

            // Create test instance
            var testInstance = Activator.CreateInstance(testMethod.ParentClass.Type);
            if (testInstance == null)
            {
                throw new InvalidOperationException($"Could not instantiate test class: {testMethod.ParentClass.Name}");
            }

            progress?.Report(new TestExecutionProgress
            {
                State = TestExecutionState.Initializing,
                PercentComplete = 10,
                Message = "Running test initialization"
            });

            // Call TestInitialize if it exists
            await CallTestLifecycleMethodAsync(testInstance, "TestInitialize", result);

            progress?.Report(new TestExecutionProgress
            {
                State = TestExecutionState.Running,
                PercentComplete = 20,
                Message = $"Running test method: {testMethod.Name}"
            });

            // Execute test method
            var sw = Stopwatch.StartNew();
            try
            {
                var methodInfo = testMethod.MethodInfo;
                if (methodInfo == null)
                {
                    throw new InvalidOperationException("Test method info is not available");
                }

                var returnValue = methodInfo.Invoke(testInstance, Array.Empty<object>());

                // Handle async test methods
                if (returnValue is Task task)
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
                    cts.CancelAfter(timeout);
                    await task.ConfigureAwait(false);
                }

                sw.Stop();
                result.Status = TestStatus.Passed;

                _logger.LogInformation("Test {TestName} passed in {Duration}ms", testMethod.Name, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.Status = TestStatus.Error;
                result.ErrorMessage = ex.Message;
                result.StackTrace = ex.StackTrace;
                _exceptionTracker.RecordException(ex, "TestMethod");
                _logger.LogError(ex, "Test {TestName} failed", testMethod.Name);
            }

            progress?.Report(new TestExecutionProgress
            {
                State = TestExecutionState.Cleaning,
                PercentComplete = 80,
                Message = "Running test cleanup"
            });

            // Call TestCleanup if it exists
            await CallTestLifecycleMethodAsync(testInstance, "TestCleanup", result);

            // Capture output and exceptions
            result.BrowserOutput = _outputCapture.GetCapturedOutput();
            result.Exceptions = _exceptionTracker.GetRecordedExceptions();

            progress?.Report(new TestExecutionProgress
            {
                State = TestExecutionState.Complete,
                PercentComplete = 100,
                Message = "Test execution complete"
            });
        }
        catch (OperationCanceledException)
        {
            result.Status = TestStatus.Error;
            result.ErrorMessage = "Test execution timeout";
            _logger.LogError("Test {TestName} timed out", testMethod.Name);
        }
        catch (Exception ex)
        {
            result.Status = TestStatus.Error;
            result.ErrorMessage = ex.Message;
            result.StackTrace = ex.StackTrace;
            _exceptionTracker.RecordException(ex, "TestExecution");
            _logger.LogError(ex, "Error executing test {TestName}", testMethod.Name);
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            _cancellationTokenSource?.Dispose();
        }

        return result;
    }

    public async Task<List<TestExecutionResult>> ExecuteTestSequenceAsync(
        List<TestMethodInfo> testMethods,
        IProgress<TestExecutionProgress>? progress = null)
    {
        var results = new List<TestExecutionResult>();

        for (int i = 0; i < testMethods.Count; i++)
        {
            var testMethod = testMethods[i];
            var percentComplete = (int)((i / (double)testMethods.Count) * 100);

            progress?.Report(new TestExecutionProgress
            {
                State = TestExecutionState.Starting,
                PercentComplete = percentComplete,
                Message = $"Executing test {i + 1} of {testMethods.Count}: {testMethod.DisplayName}"
            });

            var result = await ExecuteTestAsync(testMethod, new TestExecutionRequest(), progress);
            results.Add(result);
        }

        return results;
    }

    public void CancelExecution()
    {
        _cancellationTokenSource?.Cancel();
    }

    private async Task CallTestLifecycleMethodAsync(object testInstance, string methodName, TestExecutionResult result)
    {
        var method = testInstance.GetType().GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (method == null)
        {
            return;
        }

        try
        {
            var returnValue = method.Invoke(testInstance, Array.Empty<object>());

            if (returnValue is Task task)
            {
                await task.ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _exceptionTracker.RecordException(ex, methodName);
            _logger.LogError(ex, "Error in {MethodName}", methodName);
        }
    }
}
