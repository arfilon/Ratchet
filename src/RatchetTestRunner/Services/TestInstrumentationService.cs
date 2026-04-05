using System.Diagnostics;
using Castle.DynamicProxy;

namespace RatchetTestRunner.Services;

public class TestInstrumentationService : ITestInstrumentationService
{
    private readonly ILogger<TestInstrumentationService> _logger;
    private readonly ProxyGenerator _proxyGenerator;
    private Dictionary<Guid, StepRecorder> _stepRecorders = new();

    public TestInstrumentationService(ILogger<TestInstrumentationService> logger)
    {
        _logger = logger;
        _proxyGenerator = new ProxyGenerator();
    }

    public T WrapWithInstrumentation<T>(T instance, StepRecorder stepRecorder) where T : class
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        try
        {
            var interceptor = new InstrumentationInterceptor(stepRecorder, _logger);
            var proxy = _proxyGenerator.CreateClassProxyWithTarget(instance, interceptor);
            return proxy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create instrumentation proxy for type {Type}, returning original instance",
                typeof(T).Name);
            return instance;
        }
    }

    public StepRecorder GetOrCreateStepRecorder(Guid executionId)
    {
        if (!_stepRecorders.TryGetValue(executionId, out var recorder))
        {
            recorder = new StepRecorder();
            _stepRecorders[executionId] = recorder;
        }

        return recorder;
    }

    public void RemoveStepRecorder(Guid executionId)
    {
        _stepRecorders.Remove(executionId);
    }

    private class InstrumentationInterceptor : IInterceptor
    {
        private readonly StepRecorder _stepRecorder;
        private readonly ILogger _logger;

        public InstrumentationInterceptor(StepRecorder stepRecorder, ILogger logger)
        {
            _stepRecorder = stepRecorder;
            _logger = logger;
        }

        public void Intercept(IInvocation invocation)
        {
            var methodName = invocation.Method.Name;

            // Skip property getters/setters and private methods
            if (methodName.StartsWith("get_") || methodName.StartsWith("set_"))
            {
                invocation.Proceed();
                return;
            }

            var sw = Stopwatch.StartNew();

            try
            {
                invocation.Proceed();
                sw.Stop();

                // Extract meaningful parameters for logging
                var paramString = string.Join(", ",
                    invocation.Arguments.Select(a => a?.ToString() ?? "null").Take(3));

                var details = !string.IsNullOrEmpty(paramString) ? $"({paramString})" : null;

                _stepRecorder.RecordStep(
                    action: methodName,
                    duration: sw.Elapsed,
                    success: true,
                    details: details,
                    parameters: invocation.Arguments.FirstOrDefault()
                );

                _logger.LogDebug("Method {Method} completed in {Duration}ms", methodName, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();

                _stepRecorder.RecordStep(
                    action: methodName,
                    duration: sw.Elapsed,
                    success: false,
                    exception: ex
                );

                _logger.LogError(ex, "Method {Method} failed", methodName);
                throw;
            }
        }
    }
}
