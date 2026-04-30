using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xcalibur.Weather.Models.Testing;
using Xcalibur.Weather.Services.WeatherProvider.SunriseSunset;

namespace Xcalibur.Weather.Services.Tests.WeatherProvider
{
    /// <summary>
    /// Tests for <see cref="SunriseSunsetService"/>.
    /// </summary>
    public sealed class SunriseSunsetServiceTests
    {
        [Fact]
        public async Task GetSunriseSunsetAsync_ReturnsDeserializedResponse_WhenHttpOk()
        {
            // Arrange
            var json =
                """
                {
                  "results": {
                    "sunrise": "6:01:00 AM",
                    "sunset": "8:02:00 PM",
                    "solar_noon": "1:01:30 PM",
                    "day_length": "14:01:00",
                    "timezone": "America/New_York"
                  },
                  "status": "OK"
                }
                """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new SunriseSunsetService(http, NullLogger<SunriseSunsetService>.Instance);

            // Act
            var result = await service.GetSunriseSunsetAsync("12.34", "56.78", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Results.Should().NotBeNull();
            result.Results!.Sunrise.Should().Be("6:01:00 AM");
            result.Results.Sunset.Should().Be("8:02:00 PM");
            result.Results.DayLength.Should().Be("14:01:00");
            result.Results.SolarNoon.Should().Be("1:01:30 PM");
        }

        [Fact]
        public async Task GetSunriseSunsetAsync_ReturnsNull_WhenStatusNotSuccess()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new SunriseSunsetService(http, NullLogger<SunriseSunsetService>.Instance);

            // Act
            var result = await service.GetSunriseSunsetAsync("12.34", "56.78", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetSunriseSunsetAsync_ReturnsNull_WhenResponseInvalidJson()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid-json ", Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new SunriseSunsetService(http, NullLogger<SunriseSunsetService>.Instance);

            // Act
            var result = await service.GetSunriseSunsetAsync("12.34", "56.78", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }
    }
}
