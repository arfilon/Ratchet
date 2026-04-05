using RatchetTestRunner.Models;

namespace RatchetTestRunner.Services;

public interface ITestDiscoveryService
{
    Task<List<TestAssemblyInfo>> DiscoverAssembliesAsync(string[] assemblyPaths);
    Task<List<TestClassInfo>> GetClassesForAssemblyAsync(string assemblyPath);
    Task<List<TestMethodInfo>> GetMethodsForClassAsync(Type testClass);
    TestMethodInfo ExtractMethodMetadata(System.Reflection.MethodInfo method, TestClassInfo parentClass);
    Task RefreshDiscoveryAsync();
}
