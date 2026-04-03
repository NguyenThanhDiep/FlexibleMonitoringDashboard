using FlexibleMonitoringDashboard.Components;
using FlexibleMonitoringDashboard.Endpoints;
using FlexibleMonitoringDashboard.Services;
using MudBlazor.Services;
using System.Text.Json.Serialization;

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

// Serialize enums as strings so client DTOs can deserialize them correctly
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

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
