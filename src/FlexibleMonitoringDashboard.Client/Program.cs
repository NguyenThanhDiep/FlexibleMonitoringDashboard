using FlexibleMonitoringDashboard.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// MudBlazor
builder.Services.AddMudServices();

// HttpClient for backend API calls
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Client services (will be registered as tasks are completed)
// builder.Services.AddScoped<DashboardStateService>();
// builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddScoped<ChartTypeRecommender>();
// builder.Services.AddScoped<DataProxyClient>();

await builder.Build().RunAsync();
