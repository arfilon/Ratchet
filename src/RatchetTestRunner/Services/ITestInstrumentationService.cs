namespace RatchetTestRunner.Services;

public interface ITestInstrumentationService
{
    T WrapWithInstrumentation<T>(T instance, StepRecorder stepRecorder) where T : class;
    StepRecorder GetOrCreateStepRecorder(Guid executionId);
    void RemoveStepRecorder(Guid executionId);
}
