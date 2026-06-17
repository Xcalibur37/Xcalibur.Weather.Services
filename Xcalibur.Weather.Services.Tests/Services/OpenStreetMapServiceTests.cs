using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xcalibur.Weather.Models.Testing;
using Xcalibur.Weather.Services;

namespace Xcalibur.Weather.Services.Tests.Services
{
    /// <summary>
    /// Tests for <see cref="OpenStreetMapService"/>.
    /// </summary>
    public sealed class OpenStreetMapServiceTests
    {
        [Fact]
        public async Task GetLocationsAsync_ReturnsDeserializedResponse_WhenHttpOk()
        {
            // Arrange
            var json =
                """
                [
                  {
                    "place_id": 123,
                    "lat": "12.34",
                    "lon": "56.78",
                    "display_name": "Test Location",
                    "address": {
                      "city": "TestCity",
                      "state": "TS",
                      "country": "United States",
                      "country_code": "us"
                    }
                  }
                ]
                """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new OpenStreetMapService(http, NullLogger<OpenStreetMapService>.Instance);

            // Act
            var result = await service.GetLocationsAsync("query", "us", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result![0].DisplayName.Should().Be("Test Location");
            result[0].Lat.Should().Be("12.34");
            result[0].Lon.Should().Be("56.78");
            result[0].Address.Should().NotBeNull();
            result[0].Address!.City.Should().Be("TestCity");
        }

        [Fact]
        public async Task GetLocationsAsync_ReturnsNull_WhenStatusNotSuccess()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new OpenStreetMapService(http, NullLogger<OpenStreetMapService>.Instance);

            // Act
            var result = await service.GetLocationsAsync("query", "us", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetLocationsAsync_ReturnsNull_WhenResponseInvalidJson()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid-json ", Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new OpenStreetMapService(http, NullLogger<OpenStreetMapService>.Instance);

            // Act
            var result = await service.GetLocationsAsync("query", "us", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Constructor_AddsUserAgent_WhenMissing()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));

            // Act
            _ = new OpenStreetMapService(http, NullLogger<OpenStreetMapService>.Instance);

            // Assert
            http.DefaultRequestHeaders.UserAgent.ToString().Should().NotBeNullOrWhiteSpace();
        }
    }
}
