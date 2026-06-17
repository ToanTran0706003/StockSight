using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using StockSight.Web;
using StockSight.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API base address (overridable via wwwroot/appsettings.json -> "ApiBaseUrl").
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7080";

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddSingleton(new ApiSettings(apiBaseUrl));
builder.Services.AddScoped<StockHubClient>();
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateProvider>());

await builder.Build().RunAsync();
