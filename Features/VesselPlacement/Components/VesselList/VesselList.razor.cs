using BlazorApp.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorApp.Features.VesselPlacement.Components;

/// <summary>
/// A component that displays a list of available vessels that can be dragged to the anchorage.
/// </summary>
public partial class VesselList
{
    /// <summary>
    /// The list of available vessels to display.
    /// </summary>
    [Parameter]
    public List<AvailableVessel> AvailableVessels { get; set; } = [];

    /// <summary>
    /// The width of the anchorage grid.
    /// </summary>
    [Parameter]
    public int AnchorageWidth { get; set; }

    /// <summary>
    /// The height of the anchorage grid.
    /// </summary>
    [Parameter]
    public int AnchorageHeight { get; set; }

    /// <summary>
    /// The ID of the anchorage grid element.
    /// </summary>
    [Parameter]
    public string GridElementId { get; set; } = string.Empty;

    /// <summary>
    /// Callback invoked when a vessel is rotated.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnVesselRotate { get; set; }

    private bool HasVessels => AvailableVessels.Any();

    private async Task HandleVesselRotate(string vesselId)
    {
        await OnVesselRotate.InvokeAsync(vesselId);
    }
}
