using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RatchetTestRunner.Components;
using RatchetTestRunner.Services;
using RatchetTestRunner.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

// Register application services
builder.Services.AddScoped<ITestDiscoveryService, TestDiscoveryService>();
builder.Services.AddScoped<ITestExecutionService, TestExecutionService>();
builder.Services.AddScoped<ITestInstrumentationService, TestInstrumentationService>();
builder.Services.AddScoped<IBrowserOutputCapture, BrowserOutputCapture>();
builder.Services.AddScoped<IExceptionTracker, ExceptionTracker>();
builder.Services.AddScoped<StepRecorder>();

builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    if (builder.Environment.IsDevelopment())
    {
        config.SetMinimumLevel(LogLevel.Debug);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<TestExecutionHub>("/testexecutionhub");

app.Run();
