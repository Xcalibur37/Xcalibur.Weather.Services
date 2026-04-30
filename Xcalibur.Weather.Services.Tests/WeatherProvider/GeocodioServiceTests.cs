using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xcalibur.Weather.Models.Testing;
using Xcalibur.Weather.Services.WeatherProvider.Geocodio;

namespace Xcalibur.Weather.Services.Tests.WeatherProvider
{
    /// <summary>
    /// Tests for <see cref="GeocodioService"/>.
    /// </summary>
    public sealed class GeocodioServiceTests
    {
        [Fact]
        public async Task TestApiKey_ReturnsTrue_WhenHttpOk()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new GeocodioService(http, "DUMMY_TOKEN", NullLogger<GeocodioService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task TestApiKey_ReturnsFalse_WhenForbidden()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("forbidden")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new GeocodioService(http, "DUMMY_TOKEN", NullLogger<GeocodioService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetLocationsAsync_ReturnsDeserializedResponse_WhenHttpOk()
        {
            // Arrange - valid Geocodio JSON with one result
            var json =
                """
                {
                  "results": [
                    {
                      "address_components": {
                        "city": "TestCity",
                        "county": "TestCounty",
                        "state": "TS",
                        "zip": "99999",
                        "country": "US"
                      },
                      "location": { "lat": 12.34, "lng": 56.78 }
                    }
                  ]
                }
                """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new GeocodioService(http, "DUMMY_TOKEN", NullLogger<GeocodioService>.Instance);

            // Act
            var result = await service.GetLocationsAsync("query", "US", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Results.Should().NotBeNull();
            result.Results.Should().HaveCount(1);
            var r = result.Results![0];
            r.AddressComponents.Should().NotBeNull();
            r.AddressComponents!.City.Should().Be("TestCity");
            r.Location.Should().NotBeNull();
            r.Location!.Latitude.Should().Be((decimal)12.34);
            r.Location.Longitude.Should().Be((decimal)56.78);
        }

        [Fact]
        public async Task GetLocationsAsync_ReturnsNull_WhenStatusNotSuccess()
        {
            // Arrange - non-success status
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new GeocodioService(http, "DUMMY_TOKEN", NullLogger<GeocodioService>.Instance);

            // Act
            var result = await service.GetLocationsAsync("query", "US", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetLocationsAsync_ReturnsNull_WhenResponseInvalidJson()
        {
            // Arrange - invalid JSON content
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid-json ", Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new GeocodioService(http, "DUMMY_TOKEN", NullLogger<GeocodioService>.Instance);

            // Act
            var result = await service.GetLocationsAsync("query", "US", CancellationToken.None);

            // Assert - invalid JSON should be handled and return null
            result.Should().BeNull();
        }
    }
}