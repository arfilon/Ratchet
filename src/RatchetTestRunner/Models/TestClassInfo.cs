namespace RatchetTestRunner.Models;

public class TestClassInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Type? Type { get; set; }
    public List<TestMethodInfo> Methods { get; set; } = new();
    public bool HasTestInitialize { get; set; }
    public bool HasTestCleanup { get; set; }
}
