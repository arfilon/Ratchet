# Ratchet Test Runner

A web-based Blazor Server application for running and monitoring Ratchet framework tests with real-time progress tracking, step visualization, and detailed execution analysis.

## Features

- **Test Discovery**: Dynamically discovers test assemblies, classes, and methods using .NET Reflection
- **Sequential Execution**: Runs tests one at a time with proper isolation
- **Real-time Monitoring**: SignalR-based communication for live progress updates
- **Step Tracking**: Automatically captures test steps without modifying test code
- **Console Output**: Displays browser console logs (log, info, warn, error)
- **Exception Tracking**: Records detailed exception information with context
- **Results Display**: Shows comprehensive test results with duration, steps, and errors

## Architecture

### Core Services

- **TestDiscoveryService**: Loads test assemblies and discovers test classes/methods via reflection
- **TestExecutionService**: Orchestrates test execution with lifecycle management (TestInitialize → TestMethod → TestCleanup)
- **TestInstrumentationService**: Uses Castle.Core DynamicProxy to wrap methods for step capture
- **StepRecorder**: Records test steps with duration and status information
- **ExceptionTracker**: Captures and stores exception details with context
- **BrowserOutputCapture**: Hooks into browser console events to capture output
- **TestExecutionHub**: SignalR hub for real-time communication between backend and Blazor UI

### Data Models

- **TestAssemblyInfo**: Assembly metadata with discovered test classes
- **TestClassInfo**: Test class information including setup/cleanup methods
- **TestMethodInfo**: Individual test method details
- **TestExecutionResult**: Complete execution result with steps, exceptions, and output
- **TestStep**: Individual step information (action, duration, status)
- **BrowserConsoleMessage**: Browser console log entries
- **ExceptionInfo**: Exception details with stack traces

## Project Structure

```
RatchetTestRunner/
├── Program.cs                          # Application entry point and DI configuration
├── App.razor                           # Root component
├── appsettings.json                    # Configuration
├── Components/
│   ├── Layout/
│   │   └── MainLayout.razor            # Main layout with sidebar navigation
│   ├── Pages/
│   │   └── Home.razor                  # Main test runner page
│   ├── Routes.razor                    # Router configuration
│   └── _Imports.razor                  # Blazor component imports
├── Services/
│   ├── ITestDiscoveryService.cs        # Test discovery interface
│   ├── TestDiscoveryService.cs         # Assembly reflection implementation
│   ├── ITestExecutionService.cs        # Test execution interface
│   ├── TestExecutionService.cs         # Execution orchestration
│   ├── ITestInstrumentationService.cs  # Instrumentation interface
│   ├── TestInstrumentationService.cs   # DynamicProxy implementation
│   ├── StepRecorder.cs                 # Step recording logic
│   ├── IExceptionTracker.cs            # Exception tracking interface
│   ├── ExceptionTracker.cs             # Exception tracking implementation
│   ├── IBrowserOutputCapture.cs        # Output capture interface
│   └── BrowserOutputCapture.cs         # Console output capture
├── Hubs/
│   └── TestExecutionHub.cs             # SignalR hub for real-time updates
├── Models/                             # Data models and enums
└── wwwroot/
    └── css/
        └── app.css                     # Application styling
```

## Configuration

### appsettings.json

```json
{
  "TestRunnerConfig": {
    "AssemblySearchPaths": [
      "/home/user/Ratchet/src/Test/UnitTest/bin/Debug/net8.0"
    ],
    "MaxConcurrentTests": 1,
    "TestTimeoutMs": 120000,
    "EnableScreenshots": true,
    "ScreenshotOutputPath": "/app/screenshots",
    "StepCaptureLevel": "Detailed",
    "PreserveBrowserOutputHistory": true
  }
}
```

## Running the Application

1. **Build the project** (requires .NET 8.0.x SDK):
   ```bash
   dotnet build src/RatchetTestRunner/RatchetTestRunner.csproj
   ```

2. **Run the application**:
   ```bash
   dotnet run --project src/RatchetTestRunner/RatchetTestRunner.csproj
   ```

3. **Access the web interface**:
   Open your browser and navigate to `https://localhost:5001` (or the configured port)

## How It Works

1. **Test Discovery Phase**:
   - Application reflects on test assemblies specified in appsettings.json
   - Discovers all `[TestClass]` classes and `[TestMethod]` methods
   - Displays test hierarchy in the web UI

2. **Test Selection**:
   - User selects an assembly, test class, and specific test method
   - Or can execute multiple tests sequentially

3. **Test Execution**:
   - Fresh instance of test class is created
   - `[TestInitialize]` method is called (if exists)
   - Test method is executed with step tracking enabled
   - `[TestCleanup]` method is called (if exists)
   - All exceptions are captured and reported
   - Browser console output is captured in real-time

4. **Step Capture**:
   - Methods called on Ratchet objects are intercepted via DynamicProxy
   - Each method call is recorded as a step with:
     - Method name (action)
     - Parameters
     - Duration
     - Success/failure status
   - Call stack analysis provides context

5. **Results Display**:
   - Test status (Passed, Failed, Error)
   - Execution duration
   - Step-by-step timeline
   - Exceptions with full stack traces
   - Browser console output with color coding

## Dependencies

- **Blazor Server** (.NET 8.0): Web UI framework
- **SignalR**: Real-time communication
- **Castle.Core**: Dynamic proxy for method interception
- **Microsoft.Playwright**: Browser automation (used by Ratchet)
- **MSTest**: Test framework (used by test projects)

## Integration with Ratchet

This test runner integrates seamlessly with existing Ratchet tests:

- **No test code modifications required**: Tests run unchanged
- **Existing infrastructure preserved**: Uses same TestHost and Playwright setup
- **Compatible test assemblies**: Works with any MSTest assembly following Ratchet patterns
- **Supports async tests**: Full support for async/await test methods

## Future Enhancements

- Parallel test execution
- Test filtering and search
- Test history and trends
- Screenshot capture and display
- Custom test selectors
- Test result export (JSON, HTML)
- Performance profiling
- Test templates and generation
