using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorApp;
using BlazorApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient for general use
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register FleetApiService - needs host environment for CORS proxy in development
builder.Services.AddScoped<IFleetApiService>(sp =>
{
    var environment = sp.GetRequiredService<IWebAssemblyHostEnvironment>();
    return new FleetApiService(new HttpClient(), environment.IsDevelopment());
});

await builder.Build().RunAsync();
