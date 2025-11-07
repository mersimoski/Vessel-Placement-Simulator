using BlazorApp.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorApp.Features.VesselPlacement.Components;

/// <summary>
/// A component that displays the status of available vessels.
/// </summary>
public partial class VesselStatusPanel
{
    /// <summary>
    /// The list of available vessels to display status for.
    /// </summary>
    [Parameter]
    public List<AvailableVessel> AvailableVessels { get; set; } = [];

    private bool HasVessels => AvailableVessels.Any();

    private int TotalVessels => AvailableVessels.Count;

    private IEnumerable<VesselGroup> VesselGroups =>
        AvailableVessels
            .GroupBy(v => new { v.Dimensions.Width, v.Dimensions.Height, v.Designation })
            .Select(g => new VesselGroup
            {
                Width = g.Key.Width,
                Height = g.Key.Height,
                Designation = g.Key.Designation,
                Count = g.Count()
            });

    private class VesselGroup
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public string Designation { get; init; } = string.Empty;
        public int Count { get; init; }
    }
}
