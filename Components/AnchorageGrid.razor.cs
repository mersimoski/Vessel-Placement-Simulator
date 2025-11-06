using BlazorApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorApp.Components;

public partial class AnchorageGrid : IAsyncDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

    private ElementReference gridElement;
    private DotNetObjectReference<AnchorageGrid>? dotNetHelper;

    [Parameter] public int AnchorageWidth { get; set; }
    [Parameter] public int AnchorageHeight { get; set; }
    [Parameter] public List<PlacedVessel> PlacedVessels { get; set; } = [];
    [Parameter] public EventCallback<PlacedVessel> OnVesselPlaced { get; set; }
    [Parameter] public EventCallback<string> OnVesselRemoved { get; set; }

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
                await JsRuntime.InvokeVoidAsync("setupGridDropHandlers", gridElement, AnchorageWidth, AnchorageHeight,
                    dotNetHelper);
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
        var effectiveWidth = int.Parse(parts[1]);
        var effectiveHeight = int.Parse(parts[2]);
        var isRotated = bool.Parse(parts[3]);
        var designation = parts[4];

        // Get original dimensions (before rotation)
        // The effectiveWidth/effectiveHeight in the drag data already accounts for rotation
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
        if ((from existingVessel in PlacedVessels
                where existingVessel.Id != vessel.Id
                select existingVessel.GetOccupiedPositions())
            .Any(existingPositions => vesselPositions.Intersect(existingPositions).Any()))
        {
            return; // Collision detected, can't place
        }

        // Check if this vessel ID is already placed - if so, it means we're trying to move it
        // But since vessels are removed from available when placed, this shouldn't happen for new placements
        // Only allow moving if dragging from placed vessels (not implemented yet)
        var existing = PlacedVessels.FirstOrDefault(v => v.Id == vessel.Id);
        if (existing != null)
        {
            // Allow moving by removing the old position first
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
        // Note: x + width must be <= AnchorageWidth (not >) because if x + width == AnchorageWidth,
        // that means the vessel occupies cells from x to (x + width - 1), which is the last valid cell
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
                // Double-check bounds - only add valid positions
                // This ensures we only check positions that are actually within the grid
                if (posX >= 0 && posX < AnchorageWidth && posY >= 0 && posY < AnchorageHeight)
                {
                    tempPositions.Add((posX, posY));
                }
            }
        }

        // If no valid positions calculated, it's a collision
        // This should not happen if the bounds check passed, but it's a safety check
        if (tempPositions.Count == 0)
        {
            return Task.FromResult(true);
        }

        // Check for overlaps with existing vessels (excluding the vessel being moved if specified)
        if (PlacedVessels.Count <= 0) return Task.FromResult(false); // No collision - placement is valid
        return Task.FromResult((from existingVessel in PlacedVessels
            where excludeVesselId == null || string.IsNullOrEmpty(excludeVesselId) ||
                  existingVessel.Id != excludeVesselId
            select existingVessel.GetOccupiedPositions()).Any(existingPositions => existingPositions.Count > 0 &&
            tempPositions
                .Intersect(existingPositions)
                .Any()));
        // Collision detected
        // No collision - placement is valid
    }

    public ValueTask DisposeAsync()
    {
        dotNetHelper?.Dispose();

        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
