using BlazorApp.Components;
using BlazorApp.Models;
using BlazorApp.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorApp.Pages;

partial class VesselPlacement
{
    [Inject] private IFleetApiService FleetApiService { get; set; } = null!;

    private FleetData? fleetData;
    private readonly List<PlacedVessel> placedVessels = [];
    private List<AvailableVessel> availableVessels = [];
    private bool isLoading = true;
    private bool isComplete;
    private AnchorageGrid? anchorageGridRef;
    private string gridElementId = string.Empty;

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
            if (newGridId != gridElementId)
            {
                gridElementId = newGridId;
                StateHasChanged(); // Trigger vessels to remeasure with new grid ID
            }
        }
    }

    private async Task LoadFleetData()
    {
        isLoading = true;
        isComplete = false;
        placedVessels.Clear();
        availableVessels.Clear();

        try
        {
            fleetData = await FleetApiService.GetRandomFleetAsync();
            if (fleetData != null)
            {
                InitializeVessels();
            }
        }
        catch (Exception)
        {
            fleetData = null;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void InitializeVessels()
    {
        if (fleetData == null) return;

        var random = new Random();
        availableVessels.Clear();

        var canvasWidth = fleetData.AnchorageSize.Width;
        var canvasHeight = fleetData.AnchorageSize.Height;

        // If no fleets from API, return empty list
        if (fleetData.Fleets.Count == 0)
        {
            return;
        }

        // Create vessels based on the API response - use exact dimensions from API
        var allVessels = new List<AvailableVessel>();

        foreach (var fleet in fleetData.Fleets)
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
        availableVessels = allVessels.OrderBy(_ => random.Next()).ToList();
    }

    private void HandleVesselPlaced(PlacedVessel vessel)
    {
        // Check if vessel is already placed
        if (placedVessels.Any(pv => pv.Id == vessel.Id))
        {
            return;
        }

        // Remove the vessel from available vessels by matching ID
        var availableVessel = availableVessels.FirstOrDefault(v => v.Id == vessel.Id);
        if (availableVessel != null)
        {
            availableVessels.Remove(availableVessel);
            placedVessels.Add(vessel);
            CheckCompletion();
        }
        else
        {
            // Don't add if already placed or not found in available
            if (placedVessels.Any(pv => pv.Id == vessel.Id)) return;
            placedVessels.Add(vessel);
        }

        StateHasChanged();
    }

    private void HandleVesselRemoved(string vesselId)
    {
        var vessel = placedVessels.FirstOrDefault(v => v.Id == vesselId);
        if (vessel == null) return;
        placedVessels.Remove(vessel);
        // Add back to available vessels
        availableVessels.Add(new AvailableVessel
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
        var vessel = availableVessels.FirstOrDefault(v => v.Id == vesselId);
        if (vessel == null) return;
        vessel.IsRotated = !vessel.IsRotated;
        StateHasChanged();
    }

    private void CheckCompletion()
    {
        isComplete = availableVessels.Count == 0 && fleetData != null;
    }

    private async Task ResetAndLoadNewFleet()
    {
        await LoadFleetData();
    }
}

