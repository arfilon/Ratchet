using System.Reflection;
using RatchetTestRunner.Models;

namespace RatchetTestRunner.Services;

public class TestDiscoveryService : ITestDiscoveryService
{
    private readonly ILogger<TestDiscoveryService> _logger;
    private Dictionary<string, TestAssemblyInfo> _cache = new();

    public TestDiscoveryService(ILogger<TestDiscoveryService> logger)
    {
        _logger = logger;
    }

    public async Task<List<TestAssemblyInfo>> DiscoverAssembliesAsync(string[] assemblyPaths)
    {
        var results = new List<TestAssemblyInfo>();

        foreach (var path in assemblyPaths)
        {
            try
            {
                if (_cache.TryGetValue(path, out var cached))
                {
                    results.Add(cached);
                    continue;
                }

                if (!File.Exists(path))
                {
                    _logger.LogWarning("Assembly not found: {Path}", path);
                    continue;
                }

                var assembly = Assembly.LoadFrom(path);
                var assemblyName = assembly.GetName().Name ?? Path.GetFileNameWithoutExtension(path);

                var assemblyInfo = new TestAssemblyInfo
                {
                    Name = assemblyName,
                    Path = path,
                    DiscoveredAt = DateTime.UtcNow
                };

                // Discover test classes
                assemblyInfo.Classes = await GetClassesForAssemblyAsync(path);

                _cache[path] = assemblyInfo;
                results.Add(assemblyInfo);

                _logger.LogInformation("Discovered assembly {AssemblyName} with {ClassCount} test classes",
                    assemblyName, assemblyInfo.Classes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering assembly: {Path}", path);
            }
        }

        return results;
    }

    public async Task<List<TestClassInfo>> GetClassesForAssemblyAsync(string assemblyPath)
    {
        var classes = new List<TestClassInfo>();

        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var testClassAttribute = typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute);

            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute(testClassAttribute) != null)
                .ToList();

            foreach (var testClass in testClasses)
            {
                var classInfo = new TestClassInfo
                {
                    Name = testClass.Name,
                    FullName = testClass.FullName ?? testClass.Name,
                    Type = testClass,
                    Methods = await GetMethodsForClassAsync(testClass)
                };

                // Check for setup/teardown methods
                var testInitializeAttr = typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute);
                var testCleanupAttr = typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute);

                classInfo.HasTestInitialize = testClass.GetMethods()
                    .Any(m => m.GetCustomAttribute(testInitializeAttr) != null);
                classInfo.HasTestCleanup = testClass.GetMethods()
                    .Any(m => m.GetCustomAttribute(testCleanupAttr) != null);

                classes.Add(classInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering classes in assembly: {Path}", assemblyPath);
        }

        return classes;
    }

    public async Task<List<TestMethodInfo>> GetMethodsForClassAsync(Type testClass)
    {
        var methods = new List<TestMethodInfo>();

        try
        {
            var testMethodAttribute = typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute);
            var classInfo = new TestClassInfo
            {
                Name = testClass.Name,
                FullName = testClass.FullName ?? testClass.Name,
                Type = testClass
            };

            var testMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute(testMethodAttribute) != null)
                .ToList();

            foreach (var method in testMethods)
            {
                var methodInfo = ExtractMethodMetadata(method, classInfo);
                methods.Add(methodInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering methods in class: {ClassName}", testClass.FullName);
        }

        return methods;
    }

    public TestMethodInfo ExtractMethodMetadata(MethodInfo method, TestClassInfo parentClass)
    {
        var methodInfo = new TestMethodInfo
        {
            Name = method.Name,
            MethodInfo = method,
            DisplayName = method.Name,
            ParentClass = parentClass,
            Description = ""
        };

        // Try to get description from attributes if available
        var descriptionAttr = method.GetCustomAttribute<DescriptionAttribute>();
        if (descriptionAttr != null)
        {
            methodInfo.Description = descriptionAttr.Description;
        }

        return methodInfo;
    }

    public async Task RefreshDiscoveryAsync()
    {
        _cache.Clear();
    }
}

// Dummy DescriptionAttribute if not available - we'll use this for display
[AttributeUsage(AttributeTargets.Method)]
public class DescriptionAttribute : Attribute
{
    public string Description { get; }

    public DescriptionAttribute(string description)
    {
        Description = description;
    }
}
