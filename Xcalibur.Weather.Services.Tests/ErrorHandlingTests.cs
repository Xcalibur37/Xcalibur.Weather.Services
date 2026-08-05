using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xcalibur.Weather.Models.Testing;

namespace Xcalibur.Weather.Services.Tests
{
    /// <summary>
    /// Comprehensive error handling tests for all weather services.
    /// </summary>
    public sealed class ErrorHandlingTests
    {
        #region AtmosporeService Error Tests

        [Fact]
        public async Task AtmosporeService_GetPollenForecastAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new AtmosporeService(http, "DUMMY_KEY", NullLogger<AtmosporeService>.Instance);

            // Act
            var result = await service.GetPollenForecastAsync("39.43", "-77.80", "2026-05-27", 1, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AtmosporeService_TestApiKey_ReturnsFalse_OnException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new AtmosporeService(http, "DUMMY_KEY", NullLogger<AtmosporeService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GeocodioService Error Tests

        [Fact]
        public async Task GeocodioService_GetLocationsAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new GeocodioService(http, "DUMMY_TOKEN", NullLogger<GeocodioService>.Instance);

            // Act
            var result = await service.GetLocationsAsync("test query", "us", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GeocodioService_TestApiKey_ReturnsFalse_OnException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new GeocodioService(http, "DUMMY_TOKEN", NullLogger<GeocodioService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region IpGeoService Error Tests

        [Fact]
        public async Task IpGeoService_GetSunMoonDataAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new IpGeoService(http, "DUMMY_TOKEN", NullLogger<IpGeoService>.Instance);

            // Act
            var result = await service.GetSunMoonDataAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task IpGeoService_TestApiKey_ReturnsFalse_OnException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new IpGeoService(http, "DUMMY_TOKEN", NullLogger<IpGeoService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region OpenMeteoService Error Tests

        [Fact]
        public async Task OpenMeteoService_GetCurrentWeatherAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetCurrentWeatherAsync("39.43", "-77.80", "", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task OpenMeteoService_GetHourlyForecastAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetHourlyForecastAsync("39.43", "-77.80", 1, 0, "", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task OpenMeteoService_GetDailyForecastAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetDailyForecastAsync("39.43", "-77.80", 7, 0, "", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task OpenMeteoService_GetCurrentAirQualityAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetCurrentAirQualityAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task OpenMeteoService_GetHourlyAirQualityAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetHourlyAirQualityAsync("39.43", "-77.80", 1, 0, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region OpenStreetMapService Error Tests

        [Fact]
        public async Task OpenStreetMapService_GetLocationsAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenStreetMapService(http, NullLogger<OpenStreetMapService>.Instance);

            // Act
            var result = await service.GetLocationsAsync("test query", "us", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region SunriseSunsetService Error Tests

        [Fact]
        public async Task SunriseSunsetService_GetSunriseSunsetAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new SunriseSunsetService(http, NullLogger<SunriseSunsetService>.Instance);

            // Act
            var result = await service.GetSunriseSunsetAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region WeatherAlertService Error Tests

        [Fact]
        public async Task WeatherAlertService_GetMeteoalarmAlertsAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Act
            var result = await service.GetMeteoalarmAlertsAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task WeatherAlertService_GetNwsAlertsAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Act
            var result = await service.GetNwsAlertsAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task WeatherAlertService_GetGdacsAlertsAsync_ReturnsNull_OnHttpRequestException()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Network error"));
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Act
            var result = await service.GetGdacsAlertsAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Helper Class

        /// <summary>
        /// HttpMessageHandler that throws an exception for testing error handling.
        /// </summary>
        private sealed class ThrowingHandler : HttpMessageHandler
        {
            private readonly Exception _exception;

            public ThrowingHandler(Exception exception)
            {
                _exception = exception;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw _exception;
            }
        }

        #endregion
    }
}
