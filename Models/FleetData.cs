namespace BlazorApp.Models;

/// <summary>
/// Represents the complete fleet data response from the API.
/// </summary>
public class FleetData
{
    public AnchorageSize AnchorageSize { get; init; } = new();
    public List<Fleet> Fleets { get; set; } = [];
}

/// <summary>
/// Represents the size of the anchorage area.
/// </summary>
public class AnchorageSize
{
    public AnchorageSize() { }

    public AnchorageSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Represents a fleet of ships with the same dimensions.
/// </summary>
// ReSharper disable once UnusedType.Global - Used by JSON deserializer
public class Fleet
{
    // ReSharper disable once UnusedMember.Global - Used by JSON deserializer
    public Fleet()
    {
        // Default constructor for JSON deserialization
    }

    // ReSharper disable once UnusedMember.Global - Used by code
    public Fleet(int shipCount)
    {
        ShipCount = shipCount;
    }

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

