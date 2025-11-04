using System.Net;
using System.Net.Http;
using System.Text;
using BlazorApp.Services;
using Xunit;

namespace BlazorApp.Tests;

public class FleetApiServiceTests
{
    private const string SampleResponseJson = @"{
  ""anchorageSize"": {
    ""width"": 12,
    ""height"": 15
  },
  ""fleets"": [
    {
      ""singleShipDimensions"": { ""width"": 6, ""height"": 5 },
      ""shipDesignation"": ""LNG Unit"",
      ""shipCount"": 2
    }
  ]
}";

    [Fact]
    public async Task GetRandomFleetAsync_UsesDirectEndpoint_WhenProxyDisabled()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => SuccessResponse());
        var client = new HttpClient(handler);
        var service = new FleetApiService(client, useProxy: false);

        // Act
        var result = await service.GetRandomFleetAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(handler.Requests);
        var request = handler.Requests.Single();
        Assert.Equal("esa.instech.no", request.RequestUri!.Host);
        Assert.Equal("/api/fleets/random", request.RequestUri.AbsolutePath);
        Assert.StartsWith("https://esa.instech.no/api/fleets/random?", request.RequestUri.AbsoluteUri);
    }

    [Fact]
    public async Task GetRandomFleetAsync_UsesProxyEndpoint_WhenProxyEnabled()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => SuccessResponse());
        var client = new HttpClient(handler);
        var service = new FleetApiService(client, useProxy: true);

        // Act
        var result = await service.GetRandomFleetAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(handler.Requests);
        var request = handler.Requests.Single();
        Assert.Equal("api.allorigins.win", request.RequestUri!.Host);
        Assert.StartsWith("https://api.allorigins.win/raw?url=", request.RequestUri.AbsoluteUri);
        Assert.Contains("https%3A%2F%2Fesa.instech.no%2Fapi%2Ffleets%2Frandom", request.RequestUri.AbsoluteUri);
    }

    [Fact]
    public async Task GetRandomFleetAsync_ReturnsNull_WhenHttpRequestFails()
    {
        // Arrange - return 500 so EnsureSuccessStatusCode throws
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var service = new FleetApiService(new HttpClient(handler), useProxy: false);

        // Act
        var result = await service.GetRandomFleetAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRandomFleetAsync_ReturnsNull_WhenDeserializationFails()
    {
        // Arrange - respond with invalid JSON
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ not valid json }", Encoding.UTF8, "application/json")
        });

        var service = new FleetApiService(new HttpClient(handler), useProxy: false);

        // Act
        var result = await service.GetRandomFleetAsync();

        // Assert
        Assert.Null(result);
    }

    private static HttpResponseMessage SuccessResponse() => new(HttpStatusCode.OK)
    {
        Content = new StringContent(SampleResponseJson, Encoding.UTF8, "application/json")
    };

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public List<HttpRequestMessage> Requests { get; } = new();

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_handler(request));
        }
    }
}

