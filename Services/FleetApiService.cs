using System.Net.Http.Json;
using System.Text.Json;
using BlazorApp.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BlazorApp.Services;

/// <summary>
/// Service implementation for fetching fleet data from the ESA API.
/// Uses a CORS proxy in development to work around CORS restrictions.
/// </summary>
public class FleetApiService : IFleetApiService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _useProxy;
    private const string ApiBaseUrl = "https://esa.instech.no";
    private const string ApiPath = "/api/fleets/random";
    
    // Public CORS proxy service for development
    // Note: In production, you would want to use your own backend API to proxy requests
    private const string CorsProxyUrl = "https://api.allorigins.win/raw?url=";

    public FleetApiService(IWebAssemblyHostEnvironment? hostEnvironment = null)
    {
        _httpClient = new HttpClient();
        
        // Use CORS proxy in development when running in browser (to work around CORS restrictions)
        // In production, you should use a backend API that proxies the requests
        _useProxy = hostEnvironment?.IsDevelopment() == true;
        
        if (_useProxy)
        {
            // For proxy, we'll construct the full URL in the request
            _httpClient.BaseAddress = new Uri(CorsProxyUrl);
        }
        else
        {
            _httpClient.BaseAddress = new Uri(ApiBaseUrl);
        }
    }

    public async Task<FleetData?> GetRandomFleetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Add cache-busting parameter to ensure we get different results each time
            var randomSeed = new Random().Next();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var cacheBuster = $"?_t={timestamp}&_r={randomSeed}";
            
            string requestUrl;
            if (_useProxy)
            {
                // Use CORS proxy to bypass CORS restrictions in development
                var fullUrl = $"{ApiBaseUrl}{ApiPath}{cacheBuster}";
                var encodedUrl = Uri.EscapeDataString(fullUrl);
                requestUrl = $"{CorsProxyUrl}{encodedUrl}";
            }
            else
            {
                requestUrl = $"{ApiPath}{cacheBuster}";
            }

            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            var fleetData = JsonSerializer.Deserialize<FleetData>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            // Shuffle fleets client-side for additional randomness
            if (fleetData != null && fleetData.Fleets.Count > 0)
            {
                var random = new Random();
                fleetData.Fleets = fleetData.Fleets.OrderBy(_ => random.Next()).ToList();
            }
            
            return fleetData;
        }
        catch (HttpRequestException ex)
        {
            // Handle network/CORS errors
            Console.WriteLine($"HTTP Error: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            // Handle deserialization errors
            Console.WriteLine($"JSON Error: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            // Handle other errors
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

