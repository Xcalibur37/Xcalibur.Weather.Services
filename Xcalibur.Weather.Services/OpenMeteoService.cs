using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Xcalibur.Weather.Models.Services.OpenMeteo.CurrentAirQuality;
using Xcalibur.Weather.Models.Services.OpenMeteo.CurrentWeather;
using Xcalibur.Weather.Models.Services.OpenMeteo.DailyWeather;
using Xcalibur.Weather.Models.Services.OpenMeteo.HourlyWeather;

namespace Xcalibur.Weather.Services
{
    /// <summary>
    /// Service to interact with the Open‑Meteo weather API.
    /// </summary>
    public class OpenMeteoService
    {
        #region Fields

        private readonly HttpClient _http;
        private readonly ILogger _logger;

        // Base URLs with placeholders for latitude, longitude, and other parameters.
        private const string BaseForecastUrl = "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&timezone=auto";
        private const string BaseHistoricalUrl = "https://archive-api.open-meteo.com/v1/archive?latitude={0}&longitude={1}&start_date={2}&end_date={3}&timezone=auto";
        private const string BaseAqiUrl = "https://air-quality-api.open-meteo.com/v1/air-quality?latitude={0}&longitude={1}&timezone=auto";

        // Use source-generated context for AOT and trimming safety
        private const string CurrentForecastUrl = 
            $"{BaseForecastUrl}&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation," +
            "rain,showers,snowfall,weather_code,cloud_cover,pressure_msl,surface_pressure,wind_speed_10m,wind_direction_10m,wind_gusts_10m,is_day";
        private const string HourlyForecast48HoursUrl = 
            $"{BaseForecastUrl}&hourly=temperature_2m,relative_humidity_2m,dew_point_2m,apparent_temperature," +
            "precipitation_probability,precipitation,rain,showers,snowfall,snow_depth,weather_code,pressure_msl," +
            "surface_pressure,cloud_cover,visibility,wind_speed_10m,wind_direction_10m,wind_gusts_10m,uv_index,is_day&forecast_days=2";
        private const string DailyForecastUrl = 
            $"{BaseForecastUrl}&daily=temperature_2m_min,temperature_2m_max,weather_code,sunrise,sunset,daylight_duration," +
            "sunshine_duration,rain_sum,showers_sum,snowfall_sum,precipitation_sum,precipitation_hours,precipitation_probability_max," +
            "wind_speed_10m_max,wind_gusts_10m_max,uv_index_max&forecast_days={2}";
        private const string YesterdayForecastHourlyUrl = 
            $"{BaseHistoricalUrl}&hourly=temperature_2m,relative_humidity_2m,pressure_msl";
        private const string YesterdayForecastDailyUrl =
            $"{BaseHistoricalUrl}&daily=temperature_2m_min,temperature_2m_max,weather_code," +
            "sunrise,sunset,daylight_duration,sunshine_duration,rain_sum,showers_sum,snowfall_sum,precipitation_sum," +
            "precipitation_hours,wind_speed_10m_max,wind_gusts_10m_max";
        private const string CurrentAqiUrl =
            $"{BaseAqiUrl}&current=us_aqi,pm10,carbon_monoxide,pm2_5,nitrogen_dioxide,sulphur_dioxide," +
            "ozone,aerosol_optical_depth,dust,uv_index,uv_index_clear_sky,ammonia";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenMeteoService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logger">The logger.</param>
        public OpenMeteoService(HttpClient httpClient, ILogger logger)
        {
            _http = httpClient;
            _logger = logger;

            // Enable SSL for AOT
            _http.DefaultRequestHeaders.ConnectionClose = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the current weather data asynchronously.
        /// Deserializes the Open‑Meteo root response and returns the nested `current` object.
        /// </summary>
        public async Task<CurrentWeatherResponse?> GetCurrentWeatherAsync(string latitude, string longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(CurrentForecastUrl, latitude, longitude);
                _logger.LogDebug("Fetching current weather for ({Latitude}, {Longitude})", latitude, longitude);

                // Create and send HTTP request
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Check for non-success status code
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenMeteo API returned {StatusCode} for current weather at ({Latitude}, {Longitude})", 
                        response.StatusCode, latitude, longitude);
                    return null;
                }
                
                // Simple streaming deserialize
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync(stream, OpenMeteoJsonContext.Default.CurrentWeatherResponse, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching current weather for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Current weather request timed out or was cancelled for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize current weather response for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching current weather for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
        }

        /// <summary>
        /// Calls the Open‑Meteo hourly endpoint and deserializes the root response.
        /// Returns null on non-success HTTP response.
        /// </summary>
        public async Task<HourlyWeatherResponse?> GetHourlyForecastAsync(string latitude, string longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(HourlyForecast48HoursUrl, latitude, longitude);
                _logger.LogDebug("Fetching hourly forecast for ({Latitude}, {Longitude})", latitude, longitude);

                // Create and send HTTP request
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Check for non-success status code
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenMeteo API returned {StatusCode} for hourly forecast at ({Latitude}, {Longitude})", 
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                // Simple streaming deserialize
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync<HourlyWeatherResponse?>(stream, OpenMeteoJsonContext.Default.HourlyWeatherResponse, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching hourly forecast for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Hourly forecast request timed out or was cancelled for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize hourly forecast response for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching hourly forecast for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
        }

        /// <summary>
        /// Gets yesterday's hourly forecast asynchronous.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="dateValue">The date value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<HourlyWeatherResponse?> GetYesterdayHourlyForecastAsync(string latitude, string longitude, string dateValue, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(YesterdayForecastHourlyUrl, latitude, longitude, dateValue, dateValue);
                _logger.LogDebug("Fetching yesterday's hourly forecast for ({Latitude}, {Longitude}) on {Date}", latitude, longitude, dateValue);

                // Create and send HTTP request
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Check for non-success status code
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenMeteo API returned {StatusCode} for yesterday's hourly forecast at ({Latitude}, {Longitude}) on {Date}",
                        response.StatusCode, latitude, longitude, dateValue);
                    return null;
                }

                // Simple streaming deserialize
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync<HourlyWeatherResponse?>(stream, OpenMeteoJsonContext.Default.HourlyWeatherResponse, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching yesterday's hourly forecast for ({Latitude}, {Longitude}) on {Date}",
                    latitude, longitude, dateValue);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Yesterday's hourly forecast request timed out or was cancelled for ({Latitude}, {Longitude}) on {Date}",
                    latitude, longitude, dateValue);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize yesterday's hourly forecast response for ({Latitude}, {Longitude}) on {Date}",
                    latitude, longitude, dateValue);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching yesterday's hourly forecast for ({Latitude}, {Longitude}) on {Date}",
                    latitude, longitude, dateValue);
                return null;
            }
        }

        /// <summary>
        /// Gets daily forecast for the given coordinates and number of days.
        /// Returns null on non-success HTTP response.
        /// </summary>
        public async Task<DailyWeatherResponse?> GetDailyForecastAsync(string latitude, string longitude, int forecastDays = 7, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(DailyForecastUrl, latitude, longitude, forecastDays);
                _logger.LogDebug("Fetching {ForecastDays}-day forecast for ({Latitude}, {Longitude})", forecastDays, latitude, longitude);

                // Create and send HTTP request
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Check for non-success status code
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenMeteo API returned {StatusCode} for daily forecast at ({Latitude}, {Longitude})", 
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                // Simple streaming deserialize
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync<DailyWeatherResponse?>(stream, OpenMeteoJsonContext.Default.DailyWeatherResponse, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching daily forecast for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Daily forecast request timed out or was cancelled for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize daily forecast response for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching daily forecast for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
        }

        /// <summary>
        /// Gets yesterday's hourly forecast asynchronous.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="startDateValue">The start date value.</param>
        /// <param name="endDateValue">The end date value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<DailyWeatherResponse?> GetYesterdayDailyForecastAsync(string latitude, string longitude, string startDateValue, string endDateValue, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(YesterdayForecastDailyUrl, latitude, longitude, startDateValue, endDateValue);
                _logger.LogDebug("Fetching yesterday's daily forecast for ({Latitude}, {Longitude}) from {StartDate} to {EndDate}", latitude, longitude, startDateValue, endDateValue);

                // Create and send HTTP request
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Check for non-success status code
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenMeteo API returned {StatusCode} for yesterday's daily forecast at ({Latitude}, {Longitude}) from {StartDate} to {EndDate}",
                        response.StatusCode, latitude, longitude, startDateValue, endDateValue);
                    return null;
                }

                // Simple streaming deserialize
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync<DailyWeatherResponse?>(stream, OpenMeteoJsonContext.Default.DailyWeatherResponse, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching yesterday's daily forecast for ({Latitude}, {Longitude}) from {StartDate} to {EndDate}",
                    latitude, longitude, startDateValue, endDateValue);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Yesterday's daily forecast request timed out or was cancelled for ({Latitude}, {Longitude}) from {StartDate} to {EndDate}",
                    latitude, longitude, startDateValue, endDateValue);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize yesterday's daily forecast response for ({Latitude}, {Longitude}) from {StartDate} to {EndDate}",
                    latitude, longitude, startDateValue, endDateValue);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching yesterday's daily forecast for ({Latitude}, {Longitude}) from {StartDate} to {EndDate}",
                    latitude, longitude, startDateValue, endDateValue);
                return null;
            }
        }

        /// <summary>
        /// Gets the current air quality data asynchronously.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<AirQualityResponse?> GetCurrentAirQualityAsync(string latitude, string longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(CurrentAqiUrl, latitude, longitude);
                _logger.LogDebug("Fetching air quality data for ({Latitude}, {Longitude})", latitude, longitude);

                // Create and send HTTP request
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Check for non-success status code
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenMeteo API returned {StatusCode} for air quality at ({Latitude}, {Longitude})",
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                // Simple streaming deserialize
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync<AirQualityResponse?>(stream, OpenMeteoJsonContext.Default.AirQualityResponse, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching air quality for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Air quality request timed out or was cancelled for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize air quality response for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching air quality for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
        }

        #endregion
    }

    /// <summary>
    /// JSON serialization context for CurrentWeatherResponse.
    /// </summary>
    /// <seealso cref="System.Text.Json.Serialization.JsonSerializerContext" />
    /// <seealso cref="System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver" />
    [JsonSerializable(typeof(CurrentWeatherResponse))]
    [JsonSerializable(typeof(HourlyWeatherResponse))]
    [JsonSerializable(typeof(DailyWeatherResponse))]
    [JsonSerializable(typeof(AirQualityResponse))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    internal partial class OpenMeteoJsonContext : JsonSerializerContext { }
}