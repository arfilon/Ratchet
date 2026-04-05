namespace RatchetTestRunner.Models;

public class TestMethodInfo
{
    public string Name { get; set; } = string.Empty;
    public System.Reflection.MethodInfo? MethodInfo { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TestClassInfo? ParentClass { get; set; }
}
