using BlazorApp.Components;
using BlazorApp.Models;
using BlazorApp.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorApp.Pages;

/// <summary>
/// Main page component for the Vessel Placement application.
/// Manages the overall state and coordination between the anchorage grid and available vessels.
/// </summary>
partial class VesselPlacement
{
    [Inject] private IFleetApiService FleetApiService { get; set; } = null!;

    private AnchorageGrid? anchorageGridRef;

    // State properties

    // Public properties for binding
    private FleetData? FleetData { get; set; }

    private List<PlacedVessel> PlacedVessels { get; } = [];

    private List<AvailableVessel> AvailableVessels { get; set; } = [];

    private bool IsLoading { get; set; } = true;

    private bool IsComplete { get; set; }

    private string GridElementId { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadFleetData();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (anchorageGridRef != null)
        {
            // Wait for grid to render, then get the ID
            await Task.Delay(300);
            var newGridId = $"anchorage-grid-{anchorageGridRef.GetHashCode()}";
            if (newGridId != GridElementId)
            {
                GridElementId = newGridId;
                StateHasChanged(); // Trigger vessels to remeasure with new grid ID
            }
        }
    }

    private async Task LoadFleetData()
    {
        IsLoading = true;
        IsComplete = false;
        PlacedVessels.Clear();
        AvailableVessels.Clear();

        try
        {
            FleetData = await FleetApiService.GetRandomFleetAsync();
            if (FleetData != null)
            {
                InitializeVessels();
            }
        }
        catch (Exception)
        {
            FleetData = null;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void InitializeVessels()
    {
        if (FleetData == null) return;

        var random = new Random();
        AvailableVessels.Clear();

        var canvasWidth = FleetData.AnchorageSize.Width;
        var canvasHeight = FleetData.AnchorageSize.Height;

        // If no fleets from API, return empty list
        if (FleetData.Fleets.Count == 0)
        {
            return;
        }

        // Create vessels based on the API response - use exact dimensions from API
        var allVessels = new List<AvailableVessel>();

        foreach (var fleet in FleetData.Fleets)
        {
            var dimensions = fleet.SingleShipDimensions;
            var designation = fleet.ShipDesignation;
            var shipCount = fleet.ShipCount;

            // Check if vessel can fit in either orientation
            var fitsNormal = dimensions.Width <= canvasWidth && dimensions.Height <= canvasHeight;
            var fitsRotated = dimensions.Height <= canvasWidth && dimensions.Width <= canvasHeight;

            // Skip vessels that cannot fit in either orientation
            if (!fitsNormal && !fitsRotated)
            {
                continue;
            }

            // Create the specified number of vessels for this fleet with exact API dimensions
            for (var i = 0; i < shipCount; i++)
            {
                var uniqueId = Guid.NewGuid().ToString();

                // Randomly determine initial rotation, but ensure it fits
                var isRotated = random.Next(2) == 1;

                switch (isRotated)
                {
                    // If one orientation doesn't fit, force the other
                    case true when !fitsRotated && fitsNormal:
                        isRotated = false;
                        break;
                    case false when !fitsNormal && fitsRotated:
                        isRotated = true;
                        break;
                }

                var vessel = new AvailableVessel
                {
                    Id = uniqueId,
                    Dimensions = dimensions, // Use exact dimensions from API
                    Designation = designation,
                    IsRotated = isRotated
                };

                allVessels.Add(vessel);
            }
        }

        // Shuffle all vessels to randomize their order
        AvailableVessels = allVessels.OrderBy(_ => random.Next()).ToList();
    }

    private void HandleVesselPlaced(PlacedVessel vessel)
    {
        // Check if vessel is already placed
        if (PlacedVessels.Any(pv => pv.Id == vessel.Id))
        {
            return;
        }

        // Remove the vessel from available vessels by matching ID
        var availableVessel = AvailableVessels.FirstOrDefault(v => v.Id == vessel.Id);
        if (availableVessel != null)
        {
            AvailableVessels.Remove(availableVessel);
            PlacedVessels.Add(vessel);
            CheckCompletion();
        }
        else
        {
            // Don't add if already placed or not found in available
            if (PlacedVessels.Any(pv => pv.Id == vessel.Id)) return;
            PlacedVessels.Add(vessel);
        }

        StateHasChanged();
    }

    private void HandleVesselRemoved(string vesselId)
    {
        var vessel = PlacedVessels.FirstOrDefault(v => v.Id == vesselId);
        if (vessel == null) return;
        PlacedVessels.Remove(vessel);
        // Add back to available vessels
        AvailableVessels.Add(new AvailableVessel
        {
            Id = vesselId,
            Dimensions = vessel.Dimensions,
            Designation = vessel.Designation,
            IsRotated = vessel.IsRotated
        });
        CheckCompletion();
        StateHasChanged();
    }

    private void HandleVesselRotate(string vesselId)
    {
        var vessel = AvailableVessels.FirstOrDefault(v => v.Id == vesselId);
        if (vessel == null) return;
        vessel.IsRotated = !vessel.IsRotated;
        StateHasChanged();
    }

    private void CheckCompletion()
    {
        IsComplete = AvailableVessels.Count == 0 && FleetData != null;
    }

    private async Task ResetAndLoadNewFleet()
    {
        await LoadFleetData();
    }
}

