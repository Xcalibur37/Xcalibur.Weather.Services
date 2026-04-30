using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xcalibur.Weather.Models.Testing;
using Xcalibur.Weather.Services.WeatherProvider.OpenMeteo;

namespace Xcalibur.Weather.Services.Tests.WeatherProvider
{
    /// <summary>
    /// Tests for <see cref="OpenMeteoService"/>.
    /// </summary>
    public sealed class OpenMeteoServiceTests
    {
        [Fact]
        public async Task GetCurrentWeatherAsync_DeserializesCurrent_WhenHttpOk()
        {
            // Arrange
            var json =
                """
                {
                  "latitude": 12.34,
                  "longitude": 56.78,
                  "current": {
                    "time": "2023-01-01T12:00",
                    "interval": 1,
                    "temperature_2m": 15.5,
                    "relative_humidity_2m": 55.0,
                    "apparent_temperature": 15.0,
                    "precipitation": 0.0,
                    "rain": 0.0,
                    "showers": 0.0,
                    "snowfall": 0.0,
                    "weather_code": 0,
                    "cloud_cover": 10.0,
                    "pressure_msl": 1013.25,
                    "surface_pressure": 1015.0,
                    "wind_speed_10m": 3.3,
                    "wind_direction_10m": 180,
                    "wind_gusts_10m": 5.0,
                    "is_day": true
                  }
                }
                """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetCurrentWeatherAsync("12.34", "56.78", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Current.Should().NotBeNull();
            result.Current!.Temperature.Should().BeApproximately(15.5, 1e-6);
            result.Current.RelativeHumidity.Should().BeApproximately(55.0, 1e-6);
            result.Current.WeatherCode.Should().Be(0);
        }

        [Fact]
        public async Task GetCurrentAirQualityAsync_DeserializesCurrent_WhenHttpOk()
        {
            // Arrange
            var json =
                """
                {
                  "latitude": 12.34,
                  "longitude": 56.78,
                  "current": {
                    "time": "2023-01-01T12:00",
                    "interval": 1,
                    "us_aqi": 42,
                    "pm10": 1.2,
                    "carbon_monoxide": 0.3,
                    "pm2_5": 2.1,
                    "nitrogen_dioxide": 0.1,
                    "sulphur_dioxide": 0.0,
                    "ozone": 0.05
                  }
                }
                """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetCurrentAirQualityAsync("12.34", "56.78", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Current.Should().NotBeNull();
            result.Current!.UsAqi.Should().Be(42);
            result.Current.Pm2_5.Should().BeApproximately(2.1, 1e-6);
        }

        [Fact]
        public async Task GetHourlyForecastAsync_DeserializesHourly_WhenHttpOk()
        {
            // Arrange - minimal hourly payload with two time points
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:00");
            var later = DateTime.Now.AddHours(1).ToString("yyyy-MM-ddTHH:00");

            // Build JSON programmatically to avoid interpolation/brace escaping issues
            var hourlyObj = new
            {
                hourly = new
                {
                    time = new[] { now, later },
                    weather_code = new[] { 0, 1 },
                    temperature_2m = new[] { 10.0, 11.0 },
                    apparent_temperature = new[] { 9.5, 10.5 },
                    relative_humidity_2m = new[] { 60.0, 61.0 },
                    dew_point_2m = new[] { 5.0, 5.5 },
                    precipitation_probability = new[] { 0.0, 10.0 },
                    precipitation = new[] { 0.0, 0.1 },
                    rain = new[] { 0.0, 0.0 },
                    showers = new[] { 0.0, 0.0 },
                    snowfall = new[] { 0.0, 0.0 },
                    snow_depth = new[] { 0.0, 0.0 },
                    pressure_msl = new[] { 1013.0, 1012.5 },
                    surface_pressure = new[] { 1015.0, 1014.5 },
                    cloud_cover = new[] { 10.0, 20.0 },
                    visibility = new[] { 10000, 10000 },
                    wind_speed_10m = new[] { 3.0, 4.0 },
                    wind_direction_10m = new[] { 180, 190 },
                    wind_gusts_10m = new[] { 5.0, 6.0 }
                }
            };

            var json = JsonSerializer.Serialize(hourlyObj);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetHourlyForecastAsync("12.34", "56.78", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Hourly.Should().NotBeNull();
            result.Hourly!.Time.Should().HaveCount(2);
            result.Hourly.Temperature2m.Should().HaveCount(2);
            result.Hourly.Temperature2m![0].Should().BeApproximately(10.0, 1e-6);
        }

        [Fact]
        public async Task GetDailyForecastAsync_DeserializesDaily_WhenHttpOk()
        {
            // Arrange - minimal daily payload with two days
            var json =
                """
                {
                  "daily": {
                    "time": ["2023-01-01", "2023-01-02"],
                    "weather_code": [0, 1],
                    "temperature_2m_max": [10.0, 12.0],
                    "temperature_2m_min": [1.0, 2.0],
                    "sunrise": ["06:00", "06:01"],
                    "sunset": ["18:00", "18:01"],
                    "daylight_duration": [43200, 43200],
                    "sunshine_duration": [3600, 3600],
                    "rain_sum": [0.0, 0.5],
                    "showers_sum": [0.0, 0.1],
                    "snowfall_sum": [0.0, 0.0],
                    "precipitation_sum": [0.0, 0.5],
                    "precipitation_hours": [0.0, 1.0],
                    "precipitation_probability_max": [0.0, 10.0],
                    "wind_speed_10m_max": [5.0, 6.0],
                    "wind_gusts_10m_max": [7.0, 8.0],
                    "uv_index_max": [1.0, 2.0]
                  }
                }
                """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetDailyForecastAsync("12.34", "56.78", 2, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Daily.Should().NotBeNull();
            result.Daily!.Time.Should().HaveCount(2);
            result.Daily.TemperatureMax.Should().HaveCount(2);
            result.Daily.TemperatureMax![1].Should().BeApproximately(12.0, 1e-6);
        }

        [Fact]
        public async Task GetYesterdayForecastAsync_DeserializesHourly_WhenHttpOk()
        {
            // Arrange - hourly payload for yesterday endpoint
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:00");
            var later = DateTime.Now.AddHours(1).ToString("yyyy-MM-ddTHH:00");

            var yesterdayObj = new
            {
                hourly = new
                {
                    time = new[] { now, later },
                    temperature_2m = new[] { 8.0, 9.0 },
                    relative_humidity_2m = new[] { 70.0, 71.0 },
                    pressure_msl = new[] { 1010.0, 1009.5 }
                }
            };

            var json = JsonSerializer.Serialize(yesterdayObj);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetYesterdayForecastAsync("12.34", "56.78", "2023-01-01", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Hourly.Should().NotBeNull();
            result.Hourly!.Time.Should().HaveCount(2);
            result.Hourly.Temperature2m![0].Should().BeApproximately(8.0, 1e-6);
        }

        [Fact]
        public async Task Methods_ReturnNull_OnNonSuccessStatus()
        {
            // Arrange
            var badResponse = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("bad") };
            using var http = new HttpClient(new DelegatingHandlerStub(badResponse));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act / Assert - all endpoints return null on non-success
            (await service.GetCurrentWeatherAsync("1", "2", CancellationToken.None)).Should().BeNull();
            (await service.GetCurrentAirQualityAsync("1", "2", CancellationToken.None)).Should().BeNull();
            (await service.GetHourlyForecastAsync("1", "2", CancellationToken.None)).Should().BeNull();
            (await service.GetDailyForecastAsync("1", "2", 1, CancellationToken.None)).Should().BeNull();
            (await service.GetYesterdayForecastAsync("1", "2", "2023-01-01", CancellationToken.None)).Should().BeNull();
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_ReturnsNull_WhenResponseInvalidJson()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid-json ", Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetCurrentWeatherAsync("12.34", "56.78", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }
    }
}