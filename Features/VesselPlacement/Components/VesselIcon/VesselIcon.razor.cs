using Microsoft.AspNetCore.Components;

namespace BlazorApp.Features.VesselPlacement.Components.VesselIcon;

/// <summary>
/// A reusable vessel icon component that renders an SVG representation of a vessel.
/// </summary>
public partial class VesselIcon
{
    private const double Scale = 10.0;

    /// <summary>
    /// The width of the vessel in grid units.
    /// </summary>
    [Parameter]
    public int Width { get; set; } = 1;

    /// <summary>
    /// The height of the vessel in grid units.
    /// </summary>
    [Parameter]
    public int Height { get; set; } = 1;

    /// <summary>
    /// Additional CSS class to apply to the SVG element.
    /// </summary>
    [Parameter]
    public string? CssClass { get; set; }

    private bool IsHorizontal => Width >= Height;

    private double ViewBoxWidth => Width * Scale;

    private double ViewBoxHeight => Height * Scale;

    private int WindowCount => IsHorizontal ? Math.Min(5, Width) : Math.Min(4, Height);
}
