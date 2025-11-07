using System.Globalization;
using BlazorApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorApp.Components;

/// <summary>
/// Component that displays the anchorage grid where vessels can be placed.
/// </summary>
public sealed partial class AnchorageGrid : IAsyncDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

    private ElementReference gridElement;
    private DotNetObjectReference<AnchorageGrid>? dotNetHelper;

    [Parameter] public int AnchorageWidth { get; set; }
    [Parameter] public int AnchorageHeight { get; set; }
    [Parameter] public List<PlacedVessel> PlacedVessels { get; set; } = [];
    [Parameter] public EventCallback<PlacedVessel> OnVesselPlaced { get; set; }
    [Parameter] public EventCallback<string> OnVesselRemoved { get; set; }

    private string GridElementId => $"anchorage-grid-{GetHashCode()}";

    private IEnumerable<GridCell> GridCells
    {
        get
        {
            for (var y = 0; y < AnchorageHeight; y++)
            {
                for (var x = 0; x < AnchorageWidth; x++)
                {
                    yield return new GridCell { X = x, Y = y };
                }
            }
        }
    }

    public string GetVesselGridStyle(PlacedVessel vessel)
    {
        var colStart = vessel.X + 1;
        var colEnd = vessel.X + vessel.EffectiveWidth + 1;
        var rowStart = vessel.Y + 1;
        var rowEnd = vessel.Y + vessel.EffectiveHeight + 1;

        return $"grid-column-start: {colStart}; grid-column-end: {colEnd}; grid-row-start: {rowStart}; grid-row-end: {rowEnd};";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Setup drop handlers on first render or when dimensions change
        if (AnchorageWidth > 0 && AnchorageHeight > 0)
        {
            if (firstRender)
            {
                dotNetHelper = DotNetObjectReference.Create(this);
            }

            if (dotNetHelper != null)
            {
                // Small delay to ensure DOM is ready
                await Task.Delay(10);
                await JsRuntime.InvokeVoidAsync(
                    "setupGridDropHandlers",
                    gridElement,
                    AnchorageWidth,
                    AnchorageHeight,
                    dotNetHelper
                );
            }
        }
    }

    [JSInvokable("HandleDropFromJS")]
    public async Task HandleDropFromJs(int gridX, int gridY, string vesselDataParam)
    {
        if (string.IsNullOrEmpty(vesselDataParam))
        {
            return;
        }

        await JsRuntime.InvokeVoidAsync("dragDropHelper.clearDragData");

        // Parse vessel data
        var parts = vesselDataParam.Split('|');
        if (parts.Length < 5) return;

        var vesselId = parts[0];

        // Parse integers with an explicit culture
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var effectiveWidth))
            return;

        if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var effectiveHeight))
            return;

        // bool.Parse is culture-independent if JS sends "true"/"false"
        var isRotated = bool.Parse(parts[3]);
        var designation = parts[4];

        // Get original dimensions (before rotation)
        var originalWidth = isRotated ? effectiveHeight : effectiveWidth;
        var originalHeight = isRotated ? effectiveWidth : effectiveHeight;

        // Create vessel first to get proper EffectiveWidth/EffectiveHeight
        var vessel = new PlacedVessel
        {
            Id = vesselId,
            Dimensions = new ShipDimensions { Width = originalWidth, Height = originalHeight },
            Designation = designation,
            X = gridX,
            Y = gridY,
            IsRotated = isRotated
        };

        // Use the actual grid cell coordinates where the drop occurred
        var x = gridX;
        var y = gridY;

        // Adjust if vessel would go out of bounds using the actual effective dimensions
        var finalEffectiveWidth = vessel.EffectiveWidth;
        var finalEffectiveHeight = vessel.EffectiveHeight;

        if (x + finalEffectiveWidth > AnchorageWidth)
        {
            x = Math.Max(0, AnchorageWidth - finalEffectiveWidth);
        }

        if (y + finalEffectiveHeight > AnchorageHeight)
        {
            y = Math.Max(0, AnchorageHeight - finalEffectiveHeight);
        }

        if (x < 0) x = 0;
        if (y < 0) y = 0;

        // Update vessel position
        vessel.X = x;
        vessel.Y = y;

        // Check bounds
        if (!vessel.IsWithinBounds(AnchorageWidth, AnchorageHeight))
        {
            return;
        }

        // Check for overlaps - use the same logic as preview check
        var vesselPositions = vessel.GetOccupiedPositions();
        if (PlacedVessels
                .Select(existingVessel => existingVessel.GetOccupiedPositions())
                .Any(existingPositions => vesselPositions.Intersect(existingPositions).Any()))
        {
            return; // Collision detected, can't place
        }

        // Check if this vessel ID is already placed - if so, allow moving by removing the old position first
        var existing = PlacedVessels.FirstOrDefault(v => v.Id == vessel.Id);
        if (existing != null)
        {
            PlacedVessels.Remove(existing);
        }

        await OnVesselPlaced.InvokeAsync(vessel);
    }

    private async Task HandleVesselClick(string vesselId)
    {
        await OnVesselRemoved.InvokeAsync(vesselId);
    }

    [JSInvokable]
    public Task<bool> CheckPlacementCollision(int x, int y, int width, int height, string? excludeVesselId = null)
    {
        // Ensure we have valid dimensions first
        if (width <= 0 || height <= 0)
        {
            return Task.FromResult(true); // Invalid dimensions = collision
        }

        // Check bounds - ensure vessel fits completely within the grid
        if (x < 0 || y < 0 || x + width > AnchorageWidth || y + height > AnchorageHeight)
        {
            return Task.FromResult(true); // Out of bounds = collision
        }

        // Calculate positions this vessel would occupy
        var tempPositions = new HashSet<(int X, int Y)>();
        for (var dx = 0; dx < width; dx++)
        {
            for (var dy = 0; dy < height; dy++)
            {
                var posX = x + dx;
                var posY = y + dy;
                if (posX >= 0 && posX < AnchorageWidth && posY >= 0 && posY < AnchorageHeight)
                {
                    tempPositions.Add((posX, posY));
                }
            }
        }

        if (tempPositions.Count == 0)
        {
            return Task.FromResult(true);
        }

        // Check for overlaps with existing vessels (excluding the vessel being moved if specified)
        if (PlacedVessels.Count <= 0) return Task.FromResult(false);

        var collision = PlacedVessels
            .Where(existingVessel => string.IsNullOrEmpty(excludeVesselId) || existingVessel.Id != excludeVesselId)
            .Select(existingVessel => existingVessel.GetOccupiedPositions())
            .Any(existingPositions => existingPositions.Count > 0 &&
                                      tempPositions.Intersect(existingPositions).Any());

        return Task.FromResult(collision);
    }

    public ValueTask DisposeAsync()
    {
        dotNetHelper?.Dispose();
        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private class GridCell
    {
        public int X { get; init; }
        public int Y { get; init; }
    }
}
