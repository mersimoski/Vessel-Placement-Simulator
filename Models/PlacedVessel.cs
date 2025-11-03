namespace BlazorApp.Models;

/// <summary>
/// Represents a vessel that has been placed in the anchorage.
/// </summary>
public class PlacedVessel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ShipDimensions Dimensions { get; set; } = new();
    public string Designation { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsRotated { get; set; }
    
    /// <summary>
    /// Gets the effective width considering rotation.
    /// </summary>
    public int EffectiveWidth => IsRotated ? Dimensions.Height : Dimensions.Width;
    
    /// <summary>
    /// Gets the effective height considering rotation.
    /// </summary>
    public int EffectiveHeight => IsRotated ? Dimensions.Width : Dimensions.Height;
    
    /// <summary>
    /// Gets all occupied grid positions by this vessel.
    /// </summary>
    public HashSet<(int X, int Y)> GetOccupiedPositions()
    {
        var positions = new HashSet<(int X, int Y)>();
        for (int dx = 0; dx < EffectiveWidth; dx++)
        {
            for (int dy = 0; dy < EffectiveHeight; dy++)
            {
                positions.Add((X + dx, Y + dy));
            }
        }
        return positions;
    }
    
    /// <summary>
    /// Checks if this vessel overlaps with another vessel.
    /// </summary>
    public bool OverlapsWith(PlacedVessel other)
    {
        var thisPositions = GetOccupiedPositions();
        var otherPositions = other.GetOccupiedPositions();
        return thisPositions.Intersect(otherPositions).Any();
    }
    
    /// <summary>
    /// Checks if this vessel is within the anchorage bounds.
    /// </summary>
    public bool IsWithinBounds(int anchorageWidth, int anchorageHeight)
    {
        return X >= 0 && Y >= 0 && 
               (X + EffectiveWidth) <= anchorageWidth && 
               (Y + EffectiveHeight) <= anchorageHeight;
    }
}

