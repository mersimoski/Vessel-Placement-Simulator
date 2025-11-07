using BlazorApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorApp.Components;

/// <summary>
/// Component that displays a draggable vessel card.
/// </summary>
public partial class DraggableVessel : IAsyncDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

    private ElementReference vesselElement;
    
    [Parameter] public AvailableVessel Vessel { get; set; } = null!;
    [Parameter] public EventCallback<string> OnRotate { get; set; }
    [Parameter] public int AnchorageWidth { get; set; } = 12;
    [Parameter] public int AnchorageHeight { get; set; } = 15;
    [Parameter] public string GridElementId { get; set; } = string.Empty;
    [Parameter] public string BorderColor { get; set; } = "#dee2e6";

    private string? lastVesselData;
    private double cardWidth = 100;
    private double cardHeight = 100;
    private string? lastGridElementId;

    private string VesselCardStyle => 
        $"width: {cardWidth}px; height: {cardHeight}px; min-width: {cardWidth}px; min-height: {cardHeight}px; border-color: {BorderColor};";

    private string TooltipText =>
        $"{Vessel.Designation} ({Vessel.Dimensions.Width} × {Vessel.Dimensions.Height})";

    private string VesselData => 
        $"{Vessel.Id}|{Vessel.EffectiveWidth}|{Vessel.EffectiveHeight}|{Vessel.IsRotated}|{Vessel.Designation}";

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
        var vesselData = VesselData;

        // Always try to get actual cell size if grid ID is available
        if (!string.IsNullOrEmpty(GridElementId))
        {
            await TryGetActualCellSize();
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

    private async Task TryGetActualCellSize()
    {
        const int retries = 8;
        for (var i = 0; i < retries; i++)
        {
            try
            {
                await Task.Delay(100 + i * 50); // Progressive delay
                var cellSize = await JsRuntime.InvokeAsync<CellSize>("dragDropHelper.getGridCellSize", GridElementId);
                
                if (!(cellSize.Width > 5) || !(cellSize.Height > 5)) 
                    continue;
                
                // Use the average of width and height to ensure square cells
                var cellSizeAvg = (cellSize.Width + cellSize.Height) / 2.0;
                    // Use scaleFactor = 1.0 to match canvas grid cell size
                    const double scaleFactor = 1.0;
                    cardWidth = Vessel.EffectiveWidth * cellSizeAvg * scaleFactor;
                    cardHeight = Vessel.EffectiveHeight * cellSizeAvg * scaleFactor;

                StateHasChanged();

                // Setup drag handler after size is set
                await JsRuntime.InvokeVoidAsync("setupDragAndDrop", vesselElement, VesselData);
                lastVesselData = VesselData;
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

    private void CalculateFallbackSize()
    {
        const double gridHeightPx = 500.0;
        var cellSize = gridHeightPx / Math.Max(1, AnchorageHeight);
        // Use scaleFactor = 1.0 to match canvas grid cell size
        const double scaleFactor = 1.0;
        cardWidth = Vessel.EffectiveWidth * cellSize * scaleFactor;
        cardHeight = Vessel.EffectiveHeight * cellSize * scaleFactor;
    }

    private async Task HandleDoubleClick()
    {
        await OnRotate.InvokeAsync(Vessel.Id);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }
    
    private class CellSize
    {
        public double Width { get; init; }
        public double Height { get; init; }
    }
}

