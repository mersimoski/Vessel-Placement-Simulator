using System.Net.Http.Json;
using System.Text.Json;
using BlazorApp.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BlazorApp.Services;

/// <summary>
/// Service implementation for fetching fleet data from the ESA API.
/// Note: The API does not have CORS headers enabled, so we use a CORS proxy in development.
/// In production, you would use a backend API that proxies the requests.
/// </summary>
public class FleetApiService : IFleetApiService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _useProxy;
    private const string ApiBaseUrl = "https://esa.instech.no";
    private const string ApiPath = "/api/fleets/random";
    
    // CORS proxy is necessary because the API doesn't send CORS headers
    // This allows browser-based requests to work in development
    private const string CorsProxyUrl = "https://api.allorigins.win/raw?url=";

    public FleetApiService(IWebAssemblyHostEnvironment? hostEnvironment = null)
    {
        _httpClient = new HttpClient();
        
        // Use CORS proxy in development - the API doesn't have CORS headers enabled
        // Without this, browsers block the request due to CORS policy
        _useProxy = hostEnvironment?.IsDevelopment() == true;
        
        if (_useProxy)
        {
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
                // Use CORS proxy - encode the full API URL and pass it to the proxy
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
        catch (HttpRequestException)
        {
            // Handle network/CORS errors
            return null;
        }
        catch (JsonException)
        {
            // Handle deserialization errors
            return null;
        }
        catch (Exception)
        {
            // Handle other errors
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

