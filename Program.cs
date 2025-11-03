using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorApp;
using BlazorApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient for general use
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register FleetApiService with host environment to enable CORS proxy in development
builder.Services.AddSingleton<IFleetApiService>(sp => 
    new FleetApiService(sp.GetRequiredService<IWebAssemblyHostEnvironment>()));

await builder.Build().RunAsync();
