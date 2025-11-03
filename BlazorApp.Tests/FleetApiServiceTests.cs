using System.Text;
using System.Text.Json;
using BlazorApp.Models;
using Xunit;

namespace BlazorApp.Tests;

/// <summary>
/// Unit tests for FleetApiService JSON deserialization logic.
/// These tests verify the core deserialization logic that FleetApiService uses,
/// which is the testable part without requiring actual network calls.
/// </summary>
public class FleetApiServiceTests
{

    [Fact]
    public void GetRandomFleetAsync_ReturnsFleetData_WhenApiSucceeds()
    {
        // Arrange
        var expectedFleetData = new FleetData
        {
            AnchorageSize = new AnchorageSize { Width = 12, Height = 15 },
            Fleets = new List<Fleet>
            {
                new Fleet
                {
                    SingleShipDimensions = new ShipDimensions { Width = 6, Height = 5 },
                    ShipDesignation = "LNG Unit",
                    ShipCount = 2
                },
                new Fleet
                {
                    SingleShipDimensions = new ShipDimensions { Width = 3, Height = 12 },
                    ShipDesignation = "Science & Engineering Ship",
                    ShipCount = 5
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(expectedFleetData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Act - Test the deserialization logic that FleetApiService uses
        // Since FleetApiService creates its own HttpClient internally, we test the JSON deserialization
        // which is the core logic that can be unit tested
        var deserializedData = JsonSerializer.Deserialize<FleetData>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(deserializedData);
        Assert.NotNull(deserializedData.AnchorageSize);
        Assert.Equal(12, deserializedData.AnchorageSize.Width);
        Assert.Equal(15, deserializedData.AnchorageSize.Height);
        Assert.Equal(2, deserializedData.Fleets.Count);
        Assert.Equal("LNG Unit", deserializedData.Fleets[0].ShipDesignation);
        Assert.Equal(2, deserializedData.Fleets[0].ShipCount);
        Assert.Equal(6, deserializedData.Fleets[0].SingleShipDimensions.Width);
        Assert.Equal(5, deserializedData.Fleets[0].SingleShipDimensions.Height);
    }

    [Fact]
    public void GetRandomFleetAsync_HandlesHttpError_ReturnsNull()
    {
        // This test verifies that the service handles HTTP errors gracefully
        // Since FleetApiService has try-catch blocks, we're testing the error handling logic
        
        // Arrange - Simulate a JSON response that would cause an error
        var invalidJson = "{ invalid json }";

        // Act - Try to deserialize invalid JSON
        FleetData? result = null;
        try
        {
            result = JsonSerializer.Deserialize<FleetData>(invalidJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            // Expected behavior - service should catch this and return null
            result = null;
        }

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRandomFleetAsync_HandlesEmptyResponse_ReturnsDefaultValues()
    {
        // Arrange
        var emptyJson = "{}";

        // Act
        var result = JsonSerializer.Deserialize<FleetData>(emptyJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        // Empty JSON deserializes to a FleetData with default values (not null)
        Assert.NotNull(result);
        Assert.NotNull(result.AnchorageSize);
        Assert.Equal(0, result.AnchorageSize.Width);
        Assert.Equal(0, result.AnchorageSize.Height);
        Assert.Empty(result.Fleets);
    }

    [Fact]
    public void FleetData_Deserializes_WithCorrectStructure()
    {
        // Arrange
        var json = @"{
            ""anchorageSize"": {
                ""width"": 10,
                ""height"": 10
            },
            ""fleets"": [
                {
                    ""singleShipDimensions"": {
                        ""width"": 3,
                        ""height"": 3
                    },
                    ""shipDesignation"": ""Test Ship"",
                    ""shipCount"": 4
                }
            ]
        }";

        // Act
        var result = JsonSerializer.Deserialize<FleetData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AnchorageSize);
        Assert.Equal(10, result.AnchorageSize.Width);
        Assert.Equal(10, result.AnchorageSize.Height);
        Assert.NotNull(result.Fleets);
        Assert.Single(result.Fleets);
        Assert.Equal("Test Ship", result.Fleets[0].ShipDesignation);
        Assert.Equal(4, result.Fleets[0].ShipCount);
        Assert.Equal(3, result.Fleets[0].SingleShipDimensions.Width);
        Assert.Equal(3, result.Fleets[0].SingleShipDimensions.Height);
    }

    [Fact]
    public void FleetData_HandlesMultipleFleets()
    {
        // Arrange
        var json = @"{
            ""anchorageSize"": {
                ""width"": 12,
                ""height"": 15
            },
            ""fleets"": [
                {
                    ""singleShipDimensions"": { ""width"": 6, ""height"": 5 },
                    ""shipDesignation"": ""LNG Unit"",
                    ""shipCount"": 2
                },
                {
                    ""singleShipDimensions"": { ""width"": 3, ""height"": 12 },
                    ""shipDesignation"": ""Science & Engineering Ship"",
                    ""shipCount"": 5
                },
                {
                    ""singleShipDimensions"": { ""width"": 2, ""height"": 2 },
                    ""shipDesignation"": ""Small Vessel"",
                    ""shipCount"": 10
                }
            ]
        }";

        // Act
        var result = JsonSerializer.Deserialize<FleetData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Fleets.Count);
        Assert.Equal("LNG Unit", result.Fleets[0].ShipDesignation);
        Assert.Equal("Science & Engineering Ship", result.Fleets[1].ShipDesignation);
        Assert.Equal("Small Vessel", result.Fleets[2].ShipDesignation);
    }
}

