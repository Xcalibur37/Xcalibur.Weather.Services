using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xcalibur.Weather.Models.Testing;
using Xcalibur.Weather.Services;

namespace Xcalibur.Weather.Services.Tests.Services
{
    /// <summary>
    /// Tests for <see cref="IpGeoService"/>.
    /// </summary>
    public sealed class IpGeoServiceTests
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

            var service = new IpGeoService(http, "DUMMY_KEY", NullLogger<IpGeoService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task TestApiKey_ReturnsFalse_WhenUnauthorized()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("unauthorized")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new IpGeoService(http, "DUMMY_KEY", NullLogger<IpGeoService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetSunMoonDataAsync_ReturnsDeserializedResponse_WhenHttpOk()
        {
            // Arrange - valid Astronomy JSON
            var json =
                """
                {
                  "location": { "latitude": "12.34", "longitude": "56.78" },
                  "astronomy": {
                    "sunrise": "06:01",
                    "sunset": "18:02",
                    "day_length": "12:01:00",
                    "moonrise": "20:10",
                    "moonset": "06:05",
                    "moon_phase": "Full Moon",
                    "moon_illumination_percentage": "99",
                    "moon_angle": 12.34
                  }
                }
                """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new IpGeoService(http, "DUMMY_KEY", NullLogger<IpGeoService>.Instance);

            // Act
            var result = await service.GetSunMoonDataAsync("12.34", "56.78", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Astronomy.Should().NotBeNull();

            var astro = result.Astronomy!;
            astro.Sunrise.Should().Be("06:01");
            astro.Sunset.Should().Be("18:02");
            astro.DayLength.Should().Be("12:01:00");
            astro.Moonrise.Should().Be("20:10");
            astro.Moonset.Should().Be("06:05");
            astro.MoonPhase.Should().Be("Full Moon");
            astro.MoonIlluminationPercentage.Should().Be("99");
            astro.MoonAngle.Should().Be(12.34);
        }

        [Fact]
        public async Task GetSunMoonDataAsync_ReturnsNull_WhenStatusNotSuccess()
        {
            // Arrange - non-success status
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new IpGeoService(http, "DUMMY_KEY", NullLogger<IpGeoService>.Instance);

            // Act
            var result = await service.GetSunMoonDataAsync("12.34", "56.78", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetSunMoonDataAsync_ReturnsNull_WhenResponseInvalidJson()
        {
            // Arrange - invalid JSON content
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid-json ", Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new IpGeoService(http, "DUMMY_KEY", NullLogger<IpGeoService>.Instance);

            // Act
            var result = await service.GetSunMoonDataAsync("12.34", "56.78", CancellationToken.None);

            // Assert - invalid JSON should be handled and return null
            result.Should().BeNull();
        }
    }
}