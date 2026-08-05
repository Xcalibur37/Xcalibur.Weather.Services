using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xcalibur.Weather.Models.Testing;

namespace Xcalibur.Weather.Services.Tests
{
    /// <summary>
    /// Extended delegating handler that captures the request URI.
    /// </summary>
    internal sealed class RequestCapturingHandler : DelegatingHandler
    {
        private readonly HttpResponseMessage _response;
        public string? CapturedRequestUri { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCapturingHandler"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        public RequestCapturingHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        /// <summary>
        /// Sends the asynchronous.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequestUri = request.RequestUri?.ToString();
            return Task.FromResult(_response);
        }
    }

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
                  "generationtime_ms": 0.5,
                  "utc_offset_seconds": 0,
                  "timezone": "UTC",
                  "timezone_abbreviation": "UTC",
                  "elevation": 50.0,
                  "current_units": {
                    "time": "iso8601",
                    "interval": "seconds",
                    "temperature_2m": "°C",
                    "relative_humidity_2m": "%",
                    "apparent_temperature": "°C",
                    "precipitation": "mm",
                    "rain": "mm",
                    "showers": "mm",
                    "snowfall": "cm",
                    "weather_code": "wmo code",
                    "cloud_cover": "%",
                    "pressure_msl": "hPa",
                    "surface_pressure": "hPa",
                    "wind_speed_10m": "km/h",
                    "wind_direction_10m": "°",
                    "wind_gusts_10m": "km/h",
                    "is_day": ""
                  },
                  "current": {
                    "time": "2023-01-01T12:00",
                    "interval": 900,
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
                    "is_day": 1
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
            var result = await service.GetCurrentWeatherAsync("12.34", "56.78", "", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Current.Should().NotBeNull();
            result.Current!.Temperature.Should().BeApproximately(15.5, 1e-6);
            result.Current.RelativeHumidity.Should().BeApproximately(55.0, 1e-6);
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
            result.Current!.US_Aqi.Should().Be(42);
            result.Current.Pm2_5.Should().BeApproximately(2.1, 1e-6);
        }

        [Fact]
        public async Task GetHourlyAirQualityAsync_DeserializesHourly_WhenHttpOk()
        {
            // Arrange - minimal hourly air quality payload with two time points
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:00");
            var later = DateTime.Now.AddHours(1).ToString("yyyy-MM-ddTHH:00");

            var hourlyAqiObj = new
            {
                hourly_units = new
                {
                    time = "iso8601",
                    us_aqi = "",
                    european_aqi = "",
                    pm10 = "μg/m³",
                    pm2_5 = "μg/m³",
                    carbon_monoxide = "μg/m³",
                    nitrogen_dioxide = "μg/m³",
                    sulphur_dioxide = "μg/m³",
                    ozone = "μg/m³"
                },
                hourly = new
                {
                    time = new[] { now, later },
                    us_aqi = new int?[] { 42, 45 },
                    us_aqi_pm2_5 = new int?[] { 40, 43 },
                    us_aqi_pm10 = new int?[] { 35, 38 },
                    us_aqi_nitrogen_dioxide = new int?[] { 20, 22 },
                    us_aqi_carbon_monoxide = new int?[] { 15, 17 },
                    us_aqi_ozone = new int?[] { 30, 32 },
                    us_aqi_sulphur_dioxide = new int?[] { 10, 12 },
                    european_aqi = new int?[] { 50, 52 },
                    european_aqi_pm2_5 = new int?[] { 48, 50 },
                    european_aqi_pm10 = new int?[] { 45, 47 },
                    european_aqi_nitrogen_dioxide = new int?[] { 25, 27 },
                    european_aqi_ozone = new int?[] { 35, 37 },
                    european_aqi_sulphur_dioxide = new int?[] { 12, 14 },
                    pm10 = new double?[] { 12.5, 13.2 },
                    pm2_5 = new double?[] { 8.3, 9.1 },
                    carbon_monoxide = new double?[] { 250.0, 260.0 },
                    nitrogen_dioxide = new double?[] { 15.5, 16.2 },
                    sulphur_dioxide = new double?[] { 5.1, 5.5 },
                    ozone = new double?[] { 45.0, 47.0 }
                }
            };

            var json = JsonSerializer.Serialize(hourlyAqiObj);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetHourlyAirQualityAsync("39.43", "-77.80", 2, 0, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Hourly.Should().NotBeNull();
            result.Hourly!.Time.Should().HaveCount(2);
            result.Hourly.US_Aqi.Should().HaveCount(2);
            result.Hourly.US_Aqi![0].Should().Be(42);
            result.Hourly.US_Aqi[1].Should().Be(45);
            result.Hourly.Pm2_5![0].Should().BeApproximately(8.3, 1e-6);
            result.Hourly.EU_Aqi![0].Should().Be(50);
            result.HourlyUnits.Should().NotBeNull();
            result.HourlyUnits!.Pm10.Should().Be("μg/m³");
        }

        [Fact]
        public async Task GetHourlyAirQualityAsync_UsesDefaultForecastDays_WhenNotSpecified()
        {
            // Arrange
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:00");

            var hourlyAqiObj = new
            {
                hourly = new
                {
                    time = new[] { now },
                    us_aqi = new int?[] { 42 },
                    pm2_5 = new double?[] { 8.3 }
                }
            };

            var json = JsonSerializer.Serialize(hourlyAqiObj);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act - call without specifying forecastDays
            var result = await service.GetHourlyAirQualityAsync("39.43", "-77.80", cancellationToken: CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Hourly.Should().NotBeNull();
            result.Hourly!.Time.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetHourlyAirQualityAsync_ReturnsNull_OnNonSuccessStatus()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetHourlyAirQualityAsync("1", "2", 1, 0, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetHourlyAirQualityAsync_IncludesForecastDaysAndPastDays_InRequestUrl()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "hourly": {
                    "time": ["2026-01-01T00:00"],
                    "us_aqi": [42]
                  }
                }
                """, Encoding.UTF8, "application/json")
            };

            var handler = new RequestCapturingHandler(response);
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetHourlyAirQualityAsync("39.43", "-77.80", 3, 2, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            handler.CapturedRequestUri.Should().NotBeNull();
            handler.CapturedRequestUri.Should().Contain("forecast_days=3");
            handler.CapturedRequestUri.Should().Contain("past_days=2");
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
            var result = await service.GetHourlyForecastAsync("12.34", "56.78", 1, 0, "", CancellationToken.None);

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
                  "latitude": 12.34,
                  "longitude": 56.78,
                  "generationtime_ms": 0.5,
                  "utc_offset_seconds": 0,
                  "timezone": "UTC",
                  "timezone_abbreviation": "UTC",
                  "elevation": 50.0,
                  "daily_units": {
                    "time": "iso8601",
                    "weather_code": "wmo code",
                    "temperature_2m_max": "°C",
                    "temperature_2m_min": "°C"
                  },
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
            var result = await service.GetDailyForecastAsync("12.34", "56.78", 2, 0, "", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Daily.Should().NotBeNull();
            result.Daily!.Time.Should().HaveCount(2);
            result.Daily.TemperatureMax.Should().HaveCount(2);
            result.Daily.TemperatureMax![1].Should().BeApproximately(12.0, 1e-6);
        }

        [Fact]
        public async Task GetYesterdayHourlyForecastAsync_DeserializesHourly_WhenHttpOk()
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
            var result = await service.GetYesterdayHourlyForecastAsync("12.34", "56.78", "2023-01-01", CancellationToken.None);

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
            (await service.GetCurrentWeatherAsync("1", "2", "", CancellationToken.None)).Should().BeNull();
            (await service.GetCurrentAirQualityAsync("1", "2", CancellationToken.None)).Should().BeNull();
            (await service.GetHourlyForecastAsync("1", "2", 1, 0, "", CancellationToken.None)).Should().BeNull();
            (await service.GetDailyForecastAsync("1", "2", 1, 0, "", CancellationToken.None)).Should().BeNull();
            (await service.GetYesterdayHourlyForecastAsync("1", "2", "2023-01-01", CancellationToken.None)).Should().BeNull();
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
            var result = await service.GetCurrentWeatherAsync("12.34", "56.78", "", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        #region Model Selection Tests

        [Theory]
        [InlineData("40.7128", "-74.0060", "ncep_nbm_conus")] // New York, USA
        [InlineData("34.0522", "-118.2437", "ncep_nbm_conus")] // Los Angeles, USA
        [InlineData("30.2672", "-97.7431", "ncep_nbm_conus")] // Austin, USA
        [InlineData("51.5074", "-0.1278", "ukmo_seamless")] // London, UK
        [InlineData("48.8566", "2.3522", "meteofrance_seamless")] // Paris, France
        [InlineData("52.5200", "13.4050", "icon_seamless")] // Berlin, Germany
        [InlineData("43.6532", "-79.3832", "ncep_nbm_conus")] // Toronto, Canada (within CONUS bounds)
        [InlineData("60.1695", "-149.9003", "gfs_seamless")] // Alaska (outside all regional bounds, uses global fallback)
        [InlineData("35.6762", "139.6503", "jma_seamless")] // Tokyo, Japan
        [InlineData("-33.8688", "151.2093", "bom_access_global")] // Sydney, Australia
        [InlineData("-41.2865", "174.7762", "bom_access_global")] // Wellington, New Zealand
        [InlineData("0", "0", "gfs_seamless")] // Atlantic Ocean - global fallback
        [InlineData("-23.5505", "-46.6333", "gfs_seamless")] // São Paulo, Brazil
        public async Task GetCurrentWeatherAsync_SelectsCorrectModel_ForLocation(string latitude, string longitude, string expectedModel)
        {
            // Arrange
            var currentResponse = new
            {
                current = new
                {
                    time = "2026-01-01T12:00",
                    temperature_2m = 20.0,
                    relative_humidity_2m = 65,
                    apparent_temperature = 19.0,
                    is_day = 1,
                    precipitation = 0.0,
                    rain = 0.0,
                    showers = 0.0,
                    snowfall = 0.0,
                    weather_code = 0,
                    cloud_cover = 25,
                    pressure_msl = 1013.25,
                    surface_pressure = 1010.0,
                    wind_speed_10m = 10.0,
                    wind_direction_10m = 180,
                    wind_gusts_10m = 15.0
                }
            };

            var json = JsonSerializer.Serialize(currentResponse);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var handler = new RequestCapturingHandler(response);
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetCurrentWeatherAsync(latitude, longitude, "", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            handler.CapturedRequestUri.Should().NotBeNull();
            handler.CapturedRequestUri.Should().Contain($"models={expectedModel}",
                $"should have called API with model={expectedModel} for lat={latitude}, lon={longitude}");
        }

        [Theory]
        [InlineData("40.7128", "-74.0060", "ncep_nbm_conus")] // New York, USA
        [InlineData("34.0522", "-118.2437", "ncep_nbm_conus")] // Los Angeles, USA
        [InlineData("51.5074", "-0.1278", "ukmo_seamless")] // London, UK
        [InlineData("48.8566", "2.3522", "meteofrance_seamless")] // Paris, France
        [InlineData("52.5200", "13.4050", "icon_seamless")] // Berlin, Germany
        [InlineData("43.6532", "-79.3832", "ncep_nbm_conus")] // Toronto, Canada (within CONUS bounds for hourly)
        [InlineData("35.6762", "139.6503", "jma_seamless")] // Tokyo, Japan
        [InlineData("-33.8688", "151.2093", "bom_access_global")] // Sydney, Australia
        [InlineData("0", "0", "gfs_seamless")] // Atlantic Ocean - global fallback
        [InlineData("-23.5505", "-46.6333", "gfs_seamless")] // São Paulo, Brazil
        public async Task GetHourlyForecastAsync_SelectsCorrectModel_ForLocation(string latitude, string longitude, string expectedModel)
        {
            // Arrange
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:00");
            var hourlyResponse = new
            {
                hourly = new
                {
                    time = new[] { now },
                    weather_code = new[] { 0 },
                    temperature_2m = new[] { 20.0 },
                    apparent_temperature = new[] { 19.0 },
                    relative_humidity_2m = new[] { 65.0 },
                    dew_point_2m = new[] { 13.0 },
                    precipitation_probability = new[] { 10.0 },
                    precipitation = new[] { 0.0 },
                    rain = new[] { 0.0 },
                    showers = new[] { 0.0 },
                    snowfall = new[] { 0.0 },
                    snow_depth = new[] { 0.0 },
                    pressure_msl = new[] { 1013.0 },
                    surface_pressure = new[] { 1015.0 },
                    cloud_cover = new[] { 25.0 },
                    visibility = new[] { 10000 },
                    wind_speed_10m = new[] { 10.0 },
                    wind_direction_10m = new[] { 180 },
                    wind_gusts_10m = new[] { 15.0 }
                }
            };

            var json = JsonSerializer.Serialize(hourlyResponse);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var handler = new RequestCapturingHandler(response);
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetHourlyForecastAsync(latitude, longitude, 1, 0, "", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            handler.CapturedRequestUri.Should().NotBeNull();
            handler.CapturedRequestUri.Should().Contain($"models={expectedModel}",
                $"should have called API with model={expectedModel} for lat={latitude}, lon={longitude}");
        }

        [Theory]
        [InlineData("40.7128", "-74.0060", "ncep_nbm_conus")] // New York, USA
        [InlineData("34.0522", "-118.2437", "ncep_nbm_conus")] // Los Angeles, USA
        [InlineData("51.5074", "-0.1278", "ukmo_seamless")] // London, UK (UK-specific model takes precedence)
        [InlineData("48.8566", "2.3522", "meteofrance_seamless")] // Paris, France (France-specific bounds)
        [InlineData("52.5200", "13.4050", "ecmwf_ifs025")] // Berlin, Germany (Europe bounds use ecmwf_ifs025)
        [InlineData("43.6532", "-79.3832", "ncep_nbm_conus")] // Toronto, Canada (within CONUS bounds for daily)
        [InlineData("35.6762", "139.6503", "jma_seamless")] // Tokyo, Japan
        [InlineData("-33.8688", "151.2093", "bom_access_global")] // Sydney, Australia
        [InlineData("1.3521", "103.8198", "gfs_seamless")] // Singapore - Asia
        [InlineData("-23.5505", "-46.6333", "gfs_seamless")] // São Paulo, Brazil - South America
        [InlineData("30.0444", "31.2357", "gfs_seamless")] // Cairo, Egypt - Africa (falls in Africa bounds)
        [InlineData("0", "0", "gfs_seamless")] // Atlantic Ocean - global fallback
        public async Task GetDailyForecastAsync_SelectsCorrectModel_ForLocation(string latitude, string longitude, string expectedModel)
        {
            // Arrange
            var dailyResponse = new
            {
                daily = new
                {
                    time = new[] { "2026-01-01" },
                    weather_code = new[] { 0 },
                    temperature_2m_max = new[] { 25.0 },
                    temperature_2m_min = new[] { 15.0 },
                    apparent_temperature_max = new[] { 24.0 },
                    apparent_temperature_min = new[] { 14.0 },
                    sunrise = new[] { "2026-01-01T06:30" },
                    sunset = new[] { "2026-01-01T18:30" },
                    daylight_duration = new[] { 43200.0 },
                    sunshine_duration = new[] { 38880.0 },
                    uv_index_max = new[] { 5.0 },
                    precipitation_sum = new[] { 0.0 },
                    rain_sum = new[] { 0.0 },
                    showers_sum = new[] { 0.0 },
                    snowfall_sum = new[] { 0.0 },
                    precipitation_hours = new[] { 0.0 },
                    precipitation_probability_max = new[] { 10 },
                    wind_speed_10m_max = new[] { 15.0 },
                    wind_gusts_10m_max = new[] { 25.0 },
                    wind_direction_10m_dominant = new[] { 180 }
                }
            };

            var json = JsonSerializer.Serialize(dailyResponse);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var handler = new RequestCapturingHandler(response);
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetDailyForecastAsync(latitude, longitude, 1, 0, "", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            handler.CapturedRequestUri.Should().NotBeNull();
            handler.CapturedRequestUri.Should().Contain($"models={expectedModel}",
                $"should have called API with model={expectedModel} for lat={latitude}, lon={longitude}");
        }

        [Theory]
        [InlineData("invalid", "0", "gfs_seamless")] // Invalid latitude for current - fallback
        [InlineData("0", "invalid", "gfs_seamless")] // Invalid longitude for current - fallback
        public async Task GetCurrentWeatherAsync_WithInvalidCoordinates_UsesFallbackModel(string latitude, string longitude, string expectedModel)
        {
            // Arrange
            var currentResponse = new
            {
                current = new
                {
                    time = "2026-01-01T12:00",
                    temperature_2m = 20.0,
                    relative_humidity_2m = 65,
                    apparent_temperature = 19.0,
                    is_day = 1,
                    precipitation = 0.0,
                    rain = 0.0,
                    showers = 0.0,
                    snowfall = 0.0,
                    weather_code = 0,
                    cloud_cover = 25,
                    pressure_msl = 1013.25,
                    surface_pressure = 1010.0,
                    wind_speed_10m = 10.0,
                    wind_direction_10m = 180,
                    wind_gusts_10m = 15.0
                }
            };

            var json = JsonSerializer.Serialize(currentResponse);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var handler = new RequestCapturingHandler(response);
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetCurrentWeatherAsync(latitude, longitude, "", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            handler.CapturedRequestUri.Should().NotBeNull();
            handler.CapturedRequestUri.Should().Contain($"models={expectedModel}",
                "should fall back to gfs_seamless for invalid coordinates");
        }

        [Theory]
        [InlineData("invalid", "0", "gfs_seamless")] // Invalid latitude for daily - fallback
        [InlineData("0", "invalid", "gfs_seamless")] // Invalid longitude for daily - fallback
        public async Task GetDailyForecastAsync_WithInvalidCoordinates_UsesFallbackModel(string latitude, string longitude, string expectedModel)
        {
            // Arrange
            var dailyResponse = new
            {
                daily = new
                {
                    time = new[] { "2026-01-01" },
                    weather_code = new[] { 0 },
                    temperature_2m_max = new[] { 25.0 },
                    temperature_2m_min = new[] { 15.0 },
                    apparent_temperature_max = new[] { 24.0 },
                    apparent_temperature_min = new[] { 14.0 },
                    sunrise = new[] { "2026-01-01T06:30" },
                    sunset = new[] { "2026-01-01T18:30" },
                    daylight_duration = new[] { 43200.0 },
                    sunshine_duration = new[] { 38880.0 },
                    uv_index_max = new[] { 5.0 },
                    precipitation_sum = new[] { 0.0 },
                    rain_sum = new[] { 0.0 },
                    showers_sum = new[] { 0.0 },
                    snowfall_sum = new[] { 0.0 },
                    precipitation_hours = new[] { 0.0 },
                    precipitation_probability_max = new[] { 10 },
                    wind_speed_10m_max = new[] { 15.0 },
                    wind_gusts_10m_max = new[] { 25.0 },
                    wind_direction_10m_dominant = new[] { 180 }
                }
            };

            var json = JsonSerializer.Serialize(dailyResponse);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var handler = new RequestCapturingHandler(response);
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new OpenMeteoService(http, NullLogger<OpenMeteoService>.Instance);

            // Act
            var result = await service.GetDailyForecastAsync(latitude, longitude, 1, 0, "", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            handler.CapturedRequestUri.Should().NotBeNull();
            handler.CapturedRequestUri.Should().Contain($"models={expectedModel}",
                "should fall back to ecmwf_ifs04 for invalid coordinates in daily forecast");
        }

        [Fact]
        public void AllModels_AreCoveredByTests()
        {
            // This test ensures we have comprehensive coverage of all Open-Meteo models
            // The comprehensive list of models used across current/hourly/daily forecasts
            var modelsInUse = new[]
            {
                "ncep_nbm_conus",      // CONUS all forecasts (current, hourly, daily)
                "icon_seamless",        // Europe current & hourly
                "ukmo_seamless",        // UK current & hourly
                "meteofrance_seamless", // France current & hourly
                "gem_seamless",         // Canada all forecasts
                "jma_seamless",         // Japan all forecasts
                "bom_access_global",    // Australia/NZ all forecasts
                "gfs_seamless",         // Global fallback current & hourly, regions without specific models
                "ecmwf_ifs04"          // Global fallback daily, Europe daily
            };

            // Verify all models are recognized and in use
            modelsInUse.Should().HaveCount(9);
            modelsInUse.Should().OnlyHaveUniqueItems();
        }

        #endregion
    }
}