using FlexibleMonitoringDashboard.Components;
using FlexibleMonitoringDashboard.Endpoints;
using FlexibleMonitoringDashboard.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// MudBlazor (server-side prerender support)
builder.Services.AddMudServices();

// HttpClient for external API calls
builder.Services.AddHttpClient();

// Application services
builder.Services.AddSingleton<JsonSchemaAnalyzer>();
builder.Services.AddScoped<ExternalApiService>();

// CORS (for development)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseCors();

// API endpoints
app.MapDataProxyEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(FlexibleMonitoringDashboard.Client._Imports).Assembly);

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
