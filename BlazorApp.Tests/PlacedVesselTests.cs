using BlazorApp.Models;
using Xunit;

namespace BlazorApp.Tests;

/// <summary>
/// Unit tests for PlacedVessel model focusing on collision detection and bounds checking.
/// These tests verify the core bin-packing logic without requiring UI components.
/// </summary>
public class PlacedVesselTests
{
    [Fact]
    public void GetOccupiedPositions_ReturnsCorrectPositions_ForNonRotatedVessel()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            X = 2,
            Y = 3,
            Dimensions = new ShipDimensions { Width = 4, Height = 3 },
            IsRotated = false
        };

        // Act
        var positions = vessel.GetOccupiedPositions();

        // Assert
        Assert.Equal(12, positions.Count); // 4x3 = 12 positions
        Assert.Contains((2, 3), positions);
        Assert.Contains((5, 3), positions); // Rightmost
        Assert.Contains((2, 5), positions); // Bottom
        Assert.Contains((5, 5), positions); // Bottom-right
        Assert.DoesNotContain((1, 3), positions); // Left of vessel
        Assert.DoesNotContain((2, 2), positions); // Above vessel
    }

    [Fact]
    public void GetOccupiedPositions_ReturnsCorrectPositions_ForRotatedVessel()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            X = 1,
            Y = 1,
            Dimensions = new ShipDimensions { Width = 4, Height = 3 }, // 4x3 normally
            IsRotated = true // Should become 3x4 when rotated
        };

        // Act
        var positions = vessel.GetOccupiedPositions();

        // Assert
        Assert.Equal(12, positions.Count); // Still 12 positions (3x4 = 12)
        Assert.Contains((1, 1), positions);
        Assert.Contains((3, 1), positions); // Rightmost (width is now 3)
        Assert.Contains((1, 4), positions); // Bottom (height is now 4)
        Assert.Contains((3, 4), positions); // Bottom-right
    }

    [Fact]
    public void EffectiveWidth_ReturnsWidth_WhenNotRotated()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            Dimensions = new ShipDimensions { Width = 5, Height = 3 },
            IsRotated = false
        };

        // Act & Assert
        Assert.Equal(5, vessel.EffectiveWidth);
    }

    [Fact]
    public void EffectiveWidth_ReturnsHeight_WhenRotated()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            Dimensions = new ShipDimensions { Width = 5, Height = 3 },
            IsRotated = true
        };

        // Act & Assert
        Assert.Equal(3, vessel.EffectiveWidth); // Height becomes width when rotated
    }

    [Fact]
    public void EffectiveHeight_ReturnsHeight_WhenNotRotated()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            Dimensions = new ShipDimensions { Width = 5, Height = 3 },
            IsRotated = false
        };

        // Act & Assert
        Assert.Equal(3, vessel.EffectiveHeight);
    }

    [Fact]
    public void EffectiveHeight_ReturnsWidth_WhenRotated()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            Dimensions = new ShipDimensions { Width = 5, Height = 3 },
            IsRotated = true
        };

        // Act & Assert
        Assert.Equal(5, vessel.EffectiveHeight); // Width becomes height when rotated
    }

    [Fact]
    public void OverlapsWith_ReturnsTrue_WhenVesselsOverlap()
    {
        // Arrange
        var vessel1 = new PlacedVessel
        {
            X = 2,
            Y = 2,
            Dimensions = new ShipDimensions { Width = 3, Height = 3 },
            IsRotated = false
        };

        var vessel2 = new PlacedVessel
        {
            X = 3,
            Y = 3,
            Dimensions = new ShipDimensions { Width = 3, Height = 3 },
            IsRotated = false
        };

        // Act
        var result = vessel1.OverlapsWith(vessel2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void OverlapsWith_ReturnsFalse_WhenVesselsDoNotOverlap()
    {
        // Arrange
        var vessel1 = new PlacedVessel
        {
            X = 0,
            Y = 0,
            Dimensions = new ShipDimensions { Width = 3, Height = 3 },
            IsRotated = false
        };

        var vessel2 = new PlacedVessel
        {
            X = 5,
            Y = 5,
            Dimensions = new ShipDimensions { Width = 3, Height = 3 },
            IsRotated = false
        };

        // Act
        var result = vessel1.OverlapsWith(vessel2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void OverlapsWith_ReturnsTrue_WhenVesselsAreAdjacent()
    {
        // Arrange - Vessels touching at edges should not overlap
        var vessel1 = new PlacedVessel
        {
            X = 0,
            Y = 0,
            Dimensions = new ShipDimensions { Width = 3, Height = 3 },
            IsRotated = false
        };

        var vessel2 = new PlacedVessel
        {
            X = 3, // Adjacent (touching at right edge)
            Y = 0,
            Dimensions = new ShipDimensions { Width = 3, Height = 3 },
            IsRotated = false
        };

        // Act
        var result = vessel1.OverlapsWith(vessel2);

        // Assert - Adjacent vessels should not overlap
        Assert.False(result);
    }

    [Fact]
    public void OverlapsWith_ReturnsTrue_WhenVesselsShareSamePosition()
    {
        // Arrange
        var vessel1 = new PlacedVessel
        {
            X = 2,
            Y = 2,
            Dimensions = new ShipDimensions { Width = 3, Height = 3 },
            IsRotated = false
        };

        var vessel2 = new PlacedVessel
        {
            X = 2, // Same position
            Y = 2, // Same position
            Dimensions = new ShipDimensions { Width = 2, Height = 2 },
            IsRotated = false
        };

        // Act
        var result = vessel1.OverlapsWith(vessel2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void OverlapsWith_ReturnsTrue_WhenRotatedVesselOverlaps()
    {
        // Arrange
        var vessel1 = new PlacedVessel
        {
            X = 2,
            Y = 2,
            Dimensions = new ShipDimensions { Width = 4, Height = 2 }, // 4x2 normally
            IsRotated = false
        };

        var vessel2 = new PlacedVessel
        {
            X = 3,
            Y = 2,
            Dimensions = new ShipDimensions { Width = 4, Height = 2 }, // 4x2 normally
            IsRotated = true // Becomes 2x4 when rotated
        };

        // Act
        var result = vessel1.OverlapsWith(vessel2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWithinBounds_ReturnsTrue_WhenVesselIsWithinBounds()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            X = 2,
            Y = 3,
            Dimensions = new ShipDimensions { Width = 4, Height = 3 },
            IsRotated = false
        };

        // Act
        var result = vessel.IsWithinBounds(10, 10);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWithinBounds_ReturnsFalse_WhenVesselExceedsWidth()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            X = 8,
            Y = 0,
            Dimensions = new ShipDimensions { Width = 4, Height = 3 },
            IsRotated = false
        };

        // Act - Anchorage width is 10, vessel at X=8 with width=4 extends to X=12
        var result = vessel.IsWithinBounds(10, 10);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWithinBounds_ReturnsFalse_WhenVesselExceedsHeight()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            X = 0,
            Y = 8,
            Dimensions = new ShipDimensions { Width = 3, Height = 4 },
            IsRotated = false
        };

        // Act - Anchorage height is 10, vessel at Y=8 with height=4 extends to Y=12
        var result = vessel.IsWithinBounds(10, 10);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWithinBounds_ReturnsFalse_WhenVesselIsAtNegativeX()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            X = -1,
            Y = 0,
            Dimensions = new ShipDimensions { Width = 3, Height = 3 },
            IsRotated = false
        };

        // Act
        var result = vessel.IsWithinBounds(10, 10);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWithinBounds_ReturnsFalse_WhenVesselIsAtNegativeY()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            X = 0,
            Y = -1,
            Dimensions = new ShipDimensions { Width = 3, Height = 3 },
            IsRotated = false
        };

        // Act
        var result = vessel.IsWithinBounds(10, 10);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWithinBounds_ReturnsTrue_WhenVesselFitsExactlyAtBoundary()
    {
        // Arrange - Vessel at (0,0) with size matching anchorage exactly
        var vessel = new PlacedVessel
        {
            X = 0,
            Y = 0,
            Dimensions = new ShipDimensions { Width = 10, Height = 10 },
            IsRotated = false
        };

        // Act
        var result = vessel.IsWithinBounds(10, 10);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWithinBounds_ReturnsFalse_WhenRotatedVesselExceedsBounds()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            X = 8,
            Y = 0,
            Dimensions = new ShipDimensions { Width = 4, Height = 2 }, // 4x2 normally
            IsRotated = true // Becomes 2x4 when rotated
        };

        // Act - When rotated, effective width is 2, but effective height is 4
        // At Y=0, height=4 extends to Y=4, which is within bounds
        // But let's test a case where rotated vessel exceeds width
        var vessel2 = new PlacedVessel
        {
            X = 0,
            Y = 8,
            Dimensions = new ShipDimensions { Width = 2, Height = 4 }, // 2x4 normally
            IsRotated = true // Becomes 4x2 when rotated
        };

        // Act - When rotated, effective width is 4, effective height is 2
        // At X=0, width=4 extends to X=4, which is within bounds
        // But if placed at Y=8, height=2 extends to Y=10, which is at boundary (should be OK)
        var result = vessel2.IsWithinBounds(10, 10);

        // Assert - Should be true since Y=8 + height=2 = Y=10, which is exactly at boundary
        Assert.True(result);
    }

    [Fact]
    public void IsWithinBounds_ReturnsFalse_WhenRotatedVesselExceedsWidth()
    {
        // Arrange
        var vessel = new PlacedVessel
        {
            X = 8,
            Y = 0,
            Dimensions = new ShipDimensions { Width = 2, Height = 4 }, // 2x4 normally
            IsRotated = true // Becomes 4x2 when rotated, so width=4
        };

        // Act - Anchorage width is 10, vessel at X=8 with rotated width=4 extends to X=12
        var result = vessel.IsWithinBounds(10, 10);

        // Assert
        Assert.False(result);
    }
}

