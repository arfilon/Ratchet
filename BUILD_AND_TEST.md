# Build and Test Instructions for .NET 8 Upgrade

## Prerequisites

Ensure you have the following installed:
- **.NET 8 SDK** (version 8.0.x or later)
  - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify installation: `dotnet --version`

## Step 1: Restore NuGet Packages

Navigate to the solution directory and restore all dependencies:

```bash
cd src/
dotnet restore
```

**Expected Output:**
```
Determining projects to restore...
Restored /path/to/Ratchet/src/Ratchet/Ratchet.csproj (in X ms).
Restored /path/to/Ratchet/src/Test/WebApplication/WebApplication.csproj (in X ms).
Restored /path/to/Ratchet/src/Test/UnitTest/UnitTest.csproj (in X ms).
```

**Packages that will be restored:**
- Ratchet project:
  - Knyaz.Optimus 2.2.3
  - Microsoft.AspNetCore.TestHost 8.0.11
- WebApplication project:
  - Microsoft.VisualStudio.Web.CodeGeneration.Design 8.0.7
- UnitTest project:
  - Microsoft.NET.Test.Sdk 17.11.1
  - MSTest.TestAdapter 3.6.3
  - MSTest.TestFramework 3.6.3

## Step 2: Build the Solution

Build all projects in the solution:

```bash
dotnet build
```

**Expected Output:**
```
Microsoft (R) Build Engine version X.X.X for .NET
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
  All projects are up-to-date for restore.
  Ratchet -> /path/to/bin/Debug/net8.0/Arfilon.Ratchet.dll
  WebApplication -> /path/to/bin/Debug/net8.0/WebApplication.dll
  UnitTest -> /path/to/bin/Debug/net8.0/UnitTest.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Alternative: Build Specific Projects

```bash
# Build only Ratchet library
dotnet build Ratchet/Ratchet.csproj

# Build WebApplication
dotnet build Test/WebApplication/WebApplication.csproj

# Build UnitTest
dotnet build Test/UnitTest/UnitTest.csproj
```

### Build in Release Mode

For production builds:

```bash
dotnet build -c Release
```

## Step 3: Run Unit Tests

Execute all tests in the solution:

```bash
dotnet test
```

**Expected Output:**
```
Test run for /path/to/UnitTest.dll (.NETCoreApp,Version=v8.0)
Microsoft (R) Test Execution Command Line Tool Version X.X.X
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: < 1 s
```

### Run Tests with Detailed Output

```bash
dotnet test --verbosity detailed
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName=UnitTest.UnitTest1.Login"
```

## Validation Checklist

### ✅ Pre-Build Validation (Completed)

- [x] **XML Syntax**: All `.csproj` files have valid XML structure
- [x] **Target Framework**: All projects target `net8.0`
- [x] **Package Versions**: All NuGet packages updated to .NET 8 compatible versions
- [x] **Code Syntax**: C# files use correct .NET 8 APIs
  - `IWebHostEnvironment` instead of deprecated `IHostingEnvironment`
  - Modern routing with `UseRouting()` and `UseEndpoints()`
  - `AddControllersWithViews()` instead of deprecated `AddMvc()`

### 📝 Post-Build Verification

Once you run the commands above, verify:

- [ ] All projects restore without errors
- [ ] All projects build successfully with 0 errors, 0 warnings
- [ ] All unit tests pass (2 tests: Login, Foo)
- [ ] No deprecated API warnings
- [ ] Package generation succeeds for Ratchet library (`Arfilon.Ratchet.1.0.0.nupkg`)

## Known Considerations

### Knyaz.Optimus Compatibility

The project still uses **Knyaz.Optimus 2.2.3** (last updated 2018). While this package has not been updated for .NET 8:

- ✅ It should still work due to .NET's backward compatibility
- ⚠️ Monitor for any runtime issues or deprecation warnings
- 💡 Consider alternatives if issues arise:
  - Playwright for .NET (Microsoft-backed)
  - Puppeteer Sharp
  - Selenium WebDriver

### Potential Build Issues

If you encounter issues:

1. **Package Restore Failures**
   ```bash
   # Clear NuGet cache
   dotnet nuget locals all --clear
   dotnet restore --force
   ```

2. **Build Errors with Knyaz.Optimus**
   - The package targets older .NET versions but should be compatible
   - If issues occur, check GitHub issues: https://github.com/RusKnyaz/Optimus

3. **Test Failures**
   - Ensure WebApplication can start in test mode
   - Check for any breaking changes in ASP.NET Core 8.0 middleware

## CI/CD Integration

For automated builds in CI/CD pipelines:

```yaml
# Example GitHub Actions workflow
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '8.0.x'

- name: Restore dependencies
  run: dotnet restore
  working-directory: ./src

- name: Build
  run: dotnet build --no-restore -c Release
  working-directory: ./src

- name: Test
  run: dotnet test --no-build --verbosity normal -c Release
  working-directory: ./src
```

## Troubleshooting

### Error: SDK Not Found

```
error NETSDK1045: The current .NET SDK does not support targeting .NET 8.0.
```

**Solution**: Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0

### Error: Package Version Conflicts

If you see package version conflicts, try:

```bash
dotnet restore --force-evaluate
dotnet build --no-incremental
```

### Runtime Errors

If tests pass but runtime errors occur:
1. Check application startup logs
2. Verify all middleware is registered in correct order in `Startup.cs`
3. Check for any API changes in .NET 8: https://docs.microsoft.com/en-us/dotnet/core/compatibility/8.0

## Success Criteria

The upgrade is successful when:
✅ `dotnet restore` completes without errors
✅ `dotnet build` produces 0 errors and 0 warnings
✅ `dotnet test` shows all tests passing
✅ NuGet package `Arfilon.Ratchet.1.0.0.nupkg` is generated
✅ No security vulnerabilities reported in dependencies

---

**Last Updated**: 2025-11-17
**Migration**: .NET Core 2.1 → .NET 8 LTS
**Support**: .NET 8 LTS is supported until November 2026
