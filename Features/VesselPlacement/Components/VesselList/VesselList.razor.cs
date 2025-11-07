using BlazorApp.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorApp.Features.VesselPlacement.Components.VesselList;

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

    private bool HasVessels => AvailableVessels.Count != 0;

    // Get vessels sorted by size (area) in descending order for better layout
    private List<AvailableVessel> SortedVessels =>
        AvailableVessels
            .OrderByDescending(v => v.EffectiveWidth * v.EffectiveHeight)
            .ThenByDescending(v => Math.Max(v.EffectiveWidth, v.EffectiveHeight))
            .ToList();

    // Predefined unique colors for vessel borders
    private readonly string[] vesselColors =
    [
        "#FF6B6B", // Red
        "#4ECDC4", // Turquoise
        "#45B7D1", // Blue
        "#FFA07A", // Light Salmon
        "#98D8C8", // Mint
        "#F7DC6F", // Yellow
        "#BB8FCE", // Purple
        "#85C1E2", // Sky Blue
        "#F8B739", // Orange
        "#52B788", // Green
        "#E63946", // Dark Red
        "#457B9D", // Steel Blue
        "#E76F51", // Terracotta
        "#2A9D8F", // Teal
        "#F4A261", // Sandy Brown
        "#E9C46A", // Gold
        "#264653", // Dark Slate
        "#F06292", // Pink
        "#9575CD", // Light Purple
        "#4DB6AC"  // Cyan
    ];

    private string GetVesselColor(string vesselId)
    {
        // Use vessel ID hash to get a consistent color for each vessel
        var hash = vesselId.GetHashCode();
        var index = Math.Abs(hash) % vesselColors.Length;
        return vesselColors[index];
    }

    private async Task HandleVesselRotate(string vesselId)
    {
        await OnVesselRotate.InvokeAsync(vesselId);
    }
}
