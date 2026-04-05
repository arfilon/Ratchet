using Microsoft.AspNetCore.SignalR;
using RatchetTestRunner.Models;
using RatchetTestRunner.Services;

namespace RatchetTestRunner.Hubs;

public class TestExecutionHub : Hub
{
    private readonly ITestDiscoveryService _discoveryService;
    private readonly ITestExecutionService _executionService;
    private readonly ILogger<TestExecutionHub> _logger;

    public TestExecutionHub(
        ITestDiscoveryService discoveryService,
        ITestExecutionService executionService,
        ILogger<TestExecutionHub> logger)
    {
        _discoveryService = discoveryService;
        _executionService = executionService;
        _logger = logger;
    }

    public async Task<List<TestAssemblyInfo>> GetAvailableTests(string[] assemblyPaths)
    {
        try
        {
            _logger.LogInformation("Discovering tests from {Count} paths", assemblyPaths.Length);
            var assemblies = await _discoveryService.DiscoverAssembliesAsync(assemblyPaths);
            return assemblies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering tests");
            throw;
        }
    }

    public async Task RefreshTests()
    {
        try
        {
            await _discoveryService.RefreshDiscoveryAsync();
            await Clients.Caller.SendAsync("TestsRefreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing tests");
            throw;
        }
    }

    public async Task<TestExecutionResult> ExecuteTest(TestExecutionRequest request)
    {
        try
        {
            if (request.TestMethod == null)
            {
                throw new ArgumentNullException(nameof(request.TestMethod));
            }

            _logger.LogInformation("Starting test execution: {TestName}", request.TestMethod.DisplayName);

            var progress = new Progress<TestExecutionProgress>(async update =>
            {
                // Send progress updates to connected clients
                await Clients.Caller.SendAsync("ExecutionProgress", update);
            });

            var result = await _executionService.ExecuteTestAsync(request.TestMethod, request, progress);

            _logger.LogInformation("Test execution completed: {TestName} - Status: {Status}",
                request.TestMethod.DisplayName, result.Status);

            await Clients.Caller.SendAsync("ExecutionComplete", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing test");
            throw;
        }
    }

    public async Task<List<TestExecutionResult>> ExecuteTestSequence(List<TestExecutionRequest> requests)
    {
        try
        {
            var testMethods = requests
                .Where(r => r.TestMethod != null)
                .Select(r => r.TestMethod!)
                .ToList();

            _logger.LogInformation("Starting test sequence with {Count} tests", testMethods.Count);

            var progress = new Progress<TestExecutionProgress>(async update =>
            {
                await Clients.Caller.SendAsync("SequenceProgress", update);
            });

            var results = await _executionService.ExecuteTestSequenceAsync(testMethods, progress);

            _logger.LogInformation("Test sequence completed: {Count} tests", results.Count);

            await Clients.Caller.SendAsync("SequenceComplete", results);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing test sequence");
            throw;
        }
    }

    public void CancelExecution()
    {
        _logger.LogInformation("Test execution cancel requested");
        _executionService.CancelExecution();
    }
}
