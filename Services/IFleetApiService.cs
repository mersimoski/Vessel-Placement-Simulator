using BlazorApp.Models;

namespace BlazorApp.Services;

/// <summary>
/// Service interface for fetching fleet data from the API.
/// </summary>
public interface IFleetApiService
{
    /// <summary>
    /// Fetches random fleet data from the API.
    /// </summary>
    Task<FleetData?> GetRandomFleetAsync(CancellationToken cancellationToken = default);
}

