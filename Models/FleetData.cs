namespace BlazorApp.Models;

/// <summary>
/// Represents the complete fleet data response from the API.
/// </summary>
public class FleetData
{
    public AnchorageSize AnchorageSize { get; set; } = new();
    public List<Fleet> Fleets { get; set; } = new();
}

/// <summary>
/// Represents the size of the anchorage area.
/// </summary>
public class AnchorageSize
{
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Represents a fleet of ships with the same dimensions.
/// </summary>
public class Fleet
{
    public ShipDimensions SingleShipDimensions { get; set; } = new();
    public string ShipDesignation { get; set; } = string.Empty;
    public int ShipCount { get; set; }
}

/// <summary>
/// Represents the dimensions of a ship.
/// </summary>
public class ShipDimensions
{
    public int Width { get; set; }
    public int Height { get; set; }
}

