using BlazorApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorApp.Components;

public partial class DraggableVessel : IAsyncDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

    private ElementReference vesselElement;
    [Parameter] public AvailableVessel Vessel { get; set; } = null!;
    [Parameter] public EventCallback<string> OnRotate { get; set; }
    [Parameter] public int AnchorageWidth { get; set; } = 12;
    [Parameter] public int AnchorageHeight { get; set; } = 15;
    [Parameter] public string GridElementId { get; set; } = string.Empty;

    private string? lastVesselData;
    private double cardWidth = 100;
    private double cardHeight = 100;
    private string? lastGridElementId;

    protected override void OnParametersSet()
    {
        // Recalculate if grid ID changed
        if (GridElementId != lastGridElementId)
        {
            lastGridElementId = GridElementId;
            CalculateFallbackSize();
        }
        else if (lastGridElementId == null)
        {
            CalculateFallbackSize();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var vesselData = $"{Vessel.Id}|{Vessel.EffectiveWidth}|{Vessel.EffectiveHeight}|{Vessel.IsRotated}|{Vessel.Designation}";

        // Always try to get actual cell size if grid ID is available
        if (!string.IsNullOrEmpty(GridElementId))
        {
            const int retries = 8;
            for (var i = 0; i < retries; i++)
            {
                try
                {
                    await Task.Delay(100 + i * 50); // Progressive delay
                    var cellSize = await JsRuntime.InvokeAsync<CellSize>("dragDropHelper.getGridCellSize", GridElementId);
                    if (!(cellSize.Width > 5) || !(cellSize.Height > 5)) continue; // Valid cell size
                    // Use the average of width and height to ensure square cells
                    var cellSizeAvg = (cellSize.Width + cellSize.Height) / 2.0;
                    // Scale up by 1.3x to make vessels more visible
                    const double scaleFactor = 1.3;
                    var newWidth = Vessel.EffectiveWidth * cellSizeAvg * scaleFactor;
                    var newHeight = Vessel.EffectiveHeight * cellSizeAvg * scaleFactor;

                    // Always update to ensure accurate size
                    cardWidth = newWidth;
                    cardHeight = newHeight;
                    StateHasChanged();

                    // Setup drag handler after size is set
                    await JsRuntime.InvokeVoidAsync("setupDragAndDrop", vesselElement, vesselData);
                    lastVesselData = vesselData;
                    return; // Successfully measured
                }
                catch
                {
                    // Continue to next retry
                }
            }
            // If all retries failed, use fallback with scale
            CalculateFallbackSize();
        }
        else if (firstRender)
        {
            CalculateFallbackSize();
        }

        // Setup or update drag handler if data changed or first render
        if (firstRender || vesselData != lastVesselData)
        {
            await JsRuntime.InvokeVoidAsync("setupDragAndDrop", vesselElement, vesselData);
            lastVesselData = vesselData;
        }
    }

    private void CalculateFallbackSize()
    {
        const double gridHeightPx = 500.0;
        var cellSize = gridHeightPx / Math.Max(1, AnchorageHeight);
        // Scale up by 1.3x to make vessels more visible
        const double scaleFactor = 1.3;
        cardWidth = Vessel.EffectiveWidth * cellSize * scaleFactor;
        cardHeight = Vessel.EffectiveHeight * cellSize * scaleFactor;
    }

    // Calculate grid cell dimensions for display

    private async Task HandleDoubleClick()
    {
        await OnRotate.InvokeAsync(Vessel.Id);
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup if needed
        await Task.CompletedTask;

        // Prevent finalizer from running since cleanup is already done
        GC.SuppressFinalize(this);
    }
    private class CellSize
    {
        public double Width { get; init; }
        public double Height { get; init; }
    }
}

