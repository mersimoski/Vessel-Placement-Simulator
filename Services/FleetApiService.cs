using System.Text.Json;
using BlazorApp.Models;

namespace BlazorApp.Services;

/// <summary>
/// Service implementation for fetching fleet data from the ESA API.
/// </summary>
public class FleetApiService : IFleetApiService
{
    private readonly HttpClient httpClient;
    private readonly bool useProxy;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string ApiEndpoint = "https://esa.instech.no/api/fleets/random";
    private const string CorsProxyEndpoint = "https://api.allorigins.win/raw?url=";

    public FleetApiService(HttpClient httpClient, bool useProxy = false)
    {
        this.httpClient = httpClient;
        this.useProxy = useProxy;
    }

    public async Task<FleetData?> GetRandomFleetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Add cache-busting parameter to ensure we get different results each time
            var randomSeed = new Random().Next();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var cacheBuster = $"?_t={timestamp}&_r={randomSeed}";

            var requestUrl = useProxy
                ? $"{CorsProxyEndpoint}{Uri.EscapeDataString(ApiEndpoint + cacheBuster)}"
                : $"{ApiEndpoint}{cacheBuster}";

            var response = await httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var fleetData = await JsonSerializer.DeserializeAsync<FleetData>(responseStream, SerializerOptions, cancellationToken);

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
}

