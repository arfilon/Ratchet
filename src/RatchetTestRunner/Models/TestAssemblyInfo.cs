namespace RatchetTestRunner.Models;

public class TestAssemblyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public List<TestClassInfo> Classes { get; set; } = new();
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
}
