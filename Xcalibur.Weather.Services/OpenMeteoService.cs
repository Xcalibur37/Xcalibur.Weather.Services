using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Xcalibur.Weather.Models.Services.OpenMeteo.CurrentAirQuality;
using Xcalibur.Weather.Models.Services.OpenMeteo.CurrentWeather;
using Xcalibur.Weather.Models.Services.OpenMeteo.DailyWeather;
using Xcalibur.Weather.Models.Services.OpenMeteo.HourlyAirQuality;
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

        // Current Forecast URL
        private const string CurrentForecastUrl =
            BaseForecastUrl + "&models={2}&forecast_hours=18&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation," +
            "rain,showers,snowfall,weather_code,cloud_cover,pressure_msl,surface_pressure,wind_speed_10m,wind_direction_10m,wind_gusts_10m,is_day";

        // Hourly Forecast for the next 48 hours URL
        private const string HourlyForecast48HoursUrl =
            BaseForecastUrl + "&models={2}&hourly=temperature_2m,relative_humidity_2m,dew_point_2m,apparent_temperature," +
            "precipitation_probability,precipitation,rain,showers,snowfall,weather_code," +
            "cloud_cover,visibility,wind_speed_10m,wind_direction_10m,wind_gusts_10m&forecast_days=2";

        // Supplemental Hourly Forecast for the next 48 hours URL with additional parameters
        private const string HourlyForecast48HoursSupplementalUrl =
            BaseForecastUrl + "&models={2}&hourly=snow_depth,pressure_msl,surface_pressure,uv_index,soil_moisture_9_to_27cm," +
            "soil_moisture_27_to_81cm,is_day&forecast_days=2";

        // Yesterday's Forecast URLs for hourly and daily data
        private const string YesterdayForecastHourlyUrl =
            BaseHistoricalUrl + "&hourly=temperature_2m,relative_humidity_2m,uv_index,pressure_msl";

        // Daily Forecast URL with additional parameters for a specified number of forecast days
        private const string DailyForecastUrl =
            BaseForecastUrl + "&models={2}&daily=temperature_2m_min,temperature_2m_max,weather_code,sunrise,sunset,daylight_duration," +
            "sunshine_duration,rain_sum,showers_sum,snowfall_sum,precipitation_sum,precipitation_hours,precipitation_probability_max," +
            "wind_speed_10m_max,wind_gusts_10m_max&forecast_days={3}";

        // Supplemental Daily Forecast URL with additional parameters for a specified number of forecast days
        private const string DailyForecast48HoursSupplementalUrl =
            BaseForecastUrl + "&models={2}&daily=relative_humidity_2m_min,relative_humidity_2m_max&forecast_days={3}";

        // Yesterday's Forecast URL for daily data with additional parameters
        private const string YesterdayForecastDailyUrl =
            BaseHistoricalUrl + "&daily=temperature_2m_min,temperature_2m_max,weather_code," +
            "sunrise,sunset,daylight_duration,sunshine_duration,rain_sum,showers_sum,snowfall_sum,precipitation_sum," +
            "precipitation_hours,relative_humidity_2m_min,relative_humidity_2m_max,wind_speed_10m_max,wind_gusts_10m_max";

        // Current Air Quality URL with specific parameters for air quality indices
        private const string CurrentAqiUrl =
            BaseAqiUrl + "&current=us_aqi,pm10,carbon_monoxide,pm2_5,nitrogen_dioxide,sulphur_dioxide," +
            "ozone,aerosol_optical_depth,dust,uv_index,uv_index_clear_sky,ammonia&forecast_hours=1&cell_selection=land";

        // Hourly Air Quality URL with specific parameters for air quality indices and forecast hours
        private const string HourlyAqiUrl =
            BaseAqiUrl + "&hourly=us_aqi,us_aqi_pm2_5,us_aqi_pm10,us_aqi_nitrogen_dioxide,us_aqi_carbon_monoxide," +
            "us_aqi_ozone,us_aqi_sulphur_dioxide,european_aqi_pm2_5,european_aqi_pm10,european_aqi_nitrogen_dioxide," +
            "european_aqi_ozone,european_aqi_sulphur_dioxide,european_aqi,pm10,pm2_5,carbon_monoxide,nitrogen_dioxide," +
            "sulphur_dioxide,ozone&forecast_days={2}&past_days={3}&cell_selection=land";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenMeteoService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logger">The logger.</param>
        public OpenMeteoService(HttpClient httpClient, ILogger logger)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Methods

        #region Current Forecast

        /// <summary>
        /// Gets the current weather data asynchronously.
        /// Deserializes the Open‑Meteo root response and returns the nested `current` object.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="targetModel">An explicit target model. If one is not specified, the system determines the best default based on the location.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<CurrentWeatherResponse?> GetCurrentWeatherAsync(string latitude, string longitude, string targetModel = "", CancellationToken cancellationToken = default)
        {
            try
            {
                var model = !string.IsNullOrEmpty(targetModel) ? targetModel : GetBestCurrentOrHourlyForecastModel(latitude, longitude);
                var url = string.Format(CurrentForecastUrl, latitude, longitude, model);

                _logger.LogDebug("Fetching current weather for ({Latitude}, {Longitude}) using model {Model}", latitude, longitude, model);

                // Create and send HTTP request
                using var request = ServiceHelper.CreateRequest(url);
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

        #endregion

        #region Hourly Forecast

        /// <summary>
        /// Gets the hourly forecast for the next 48 hours asynchronously.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="targetModel">An explicit target model. If one is not specified, the system determines the best default based on the location.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<HourlyWeatherResponse?> GetHourlyForecastAsync(string latitude, string longitude, string targetModel = "", CancellationToken cancellationToken = default)
        {
            var model = !string.IsNullOrEmpty(targetModel) ? targetModel : GetBestCurrentOrHourlyForecastModel(latitude, longitude);
            return await GetHourlyForecastInternalAsync(model, latitude, longitude, HourlyForecast48HoursUrl, "Hourly Forecast", cancellationToken);
        }

        /// <summary>
        /// Gets the supplemental hourly forecast for the next 48 hours asynchronously.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<HourlyWeatherResponse?> GetHourlyForecastSupplementalAsync(string latitude, string longitude, CancellationToken cancellationToken = default)
            => await GetHourlyForecastInternalAsync("best_match", latitude, longitude, HourlyForecast48HoursSupplementalUrl, "Supplemental Hourly Forecast", cancellationToken);

        /// <summary>
        /// Calls the Open‑Meteo hourly endpoint and deserializes the root response.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="urlTemplate">The URL template.</param>
        /// <param name="title">The title.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private async Task<HourlyWeatherResponse?> GetHourlyForecastInternalAsync(string model, string latitude, string longitude, string urlTemplate, string title, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(urlTemplate, latitude, longitude, model);
                _logger.LogDebug("Fetching {Title} for ({Latitude}, {Longitude}) using model {Model}", title, latitude, longitude, model);

                // Create and send HTTP request
                using var request = ServiceHelper.CreateRequest(url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Check for non-success status code
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenMeteo API returned {StatusCode} for {Title} at ({Latitude}, {Longitude})",
                        response.StatusCode, title, latitude, longitude);
                    return null;
                }

                // Simple streaming deserialize
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync<HourlyWeatherResponse?>(stream, OpenMeteoJsonContext.Default.HourlyWeatherResponse, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching {Title} for ({Latitude}, {Longitude})", title, latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "{Title} request timed out or was cancelled for ({Latitude}, {Longitude})", title, latitude, longitude);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize {Title} response for ({Latitude}, {Longitude})", title, latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching {Title} for ({Latitude}, {Longitude})", title, latitude, longitude);
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
                using var request = ServiceHelper.CreateRequest(url);
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

        #endregion

        #region Daily Forecast

        /// <summary>
        /// Gets daily forecast for the given coordinates and number of days.
        /// Returns null on non-success HTTP response.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="forecastDays">The forecast days.</param>
        /// <param name="targetModel">An explicit target model. If one is not specified, the system determines the best default based on the location.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<DailyWeatherResponse?> GetDailyForecastAsync(string latitude, string longitude, int forecastDays = 7, string targetModel = "", CancellationToken cancellationToken = default)
        {
            var model = !string.IsNullOrEmpty(targetModel) ? targetModel : GetBestDailyForecastModel(latitude, longitude);
            return await GetDailyForecastInternalAsync(model, latitude, longitude, forecastDays, DailyForecastUrl, "Daily Forecast", cancellationToken);

        }

        /// <summary>
        /// Gets the supplemental hourly forecast for the next 48 hours asynchronously.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="forecastDays">The forecast days.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<DailyWeatherResponse?> GetDailyForecastSupplementalAsync(string latitude, string longitude, int forecastDays = 7, CancellationToken cancellationToken = default)
            => await GetDailyForecastInternalAsync("gfs_seamless", latitude, longitude, forecastDays, DailyForecast48HoursSupplementalUrl, "Supplemental Daily Forecast", cancellationToken);

        /// <summary>
        /// Gets the daily forecast for the given coordinates and number of days using a specific model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="forecastDays">The forecast days.</param>
        /// <param name="urlTemplate">The URL template.</param>
        /// <param name="title">The title.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private async Task<DailyWeatherResponse?> GetDailyForecastInternalAsync(string model, string latitude, string longitude, int forecastDays, string urlTemplate, string title, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(urlTemplate, latitude, longitude, model, forecastDays);
                _logger.LogDebug("Fetching {ForecastDays}-day forecast for ({Latitude}, {Longitude}) using model {Model}", forecastDays, latitude, longitude, model);

                // Create and send HTTP request
                using var request = ServiceHelper.CreateRequest(url);
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
                using var request = ServiceHelper.CreateRequest(url);
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

        #endregion

        #region Air Quality Forecast

        /// <summary>
        /// Gets the current air quality data asynchronously.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<CurrentAirQualityResponse?> GetCurrentAirQualityAsync(string latitude, string longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(CurrentAqiUrl, latitude, longitude);
                _logger.LogDebug("Fetching air quality data for ({Latitude}, {Longitude})", latitude, longitude);

                // Create and send HTTP request
                using var request = ServiceHelper.CreateRequest(url);
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
                return await JsonSerializer.DeserializeAsync<CurrentAirQualityResponse?>(stream, OpenMeteoJsonContext.Default.CurrentAirQualityResponse, cancellationToken);
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

        /// <summary>
        /// Gets the hourly air quality forecast data asynchronously.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="forecastDays">The forecast days.</param>
        /// <param name="pastDays">The past days.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<HourlyAirQualityResponse?> GetHourlyAirQualityAsync(string latitude, string longitude, int forecastDays = 1, int pastDays = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(HourlyAqiUrl, latitude, longitude, forecastDays, pastDays);
                _logger.LogDebug("Fetching hourly air quality data for ({Latitude}, {Longitude}) with {ForecastDays} forecast days and {PastDays} past days", latitude, longitude, forecastDays, pastDays);

                // Create and send HTTP request
                using var request = ServiceHelper.CreateRequest(url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Check for non-success status code
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenMeteo API returned {StatusCode} for hourly air quality at ({Latitude}, {Longitude})",
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                // Simple streaming deserialize
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync<HourlyAirQualityResponse?>(stream, OpenMeteoJsonContext.Default.HourlyAirQualityResponse, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching hourly air quality for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Hourly air quality request timed out or was cancelled for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize hourly air quality response for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching hourly air quality for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
        }

        #endregion

        #region Model Selection

        /// <summary>
        /// Determines the best Open-Meteo model for current weather and hourly forecasts based on location.
        /// Uses region-specific models for optimal accuracy.
        /// </summary>
        /// <param name="latitude">The latitude as a string.</param>
        /// <param name="longitude">The longitude as a string.</param>
        /// <returns>The model name to use for the API request.</returns>
        private static string GetBestCurrentOrHourlyForecastModel(string latitude, string longitude)
        {
            if (!double.TryParse(latitude, out var lat) || !double.TryParse(longitude, out var lon))
                return "gfs_seamless"; // Global fallback

            // Use region-specific models for better accuracy
            return lat switch
            {
                // Continental United States (CONUS) - NBM blends multiple models
                >= 24.0 and <= 49.5 when lon is >= -125.0 and <= -66.0 => "ncep_nbm_conus",
                // United Kingdom and Ireland - UK Met Office high-resolution model
                >= 49.0 and <= 61.0 when lon is >= -11.0 and <= 3.0 => "ukmo_seamless",
                // France and nearby regions - Météo-France AROME/ARPEGE
                >= 41.0 and <= 52.0 when lon is >= -6.0 and <= 10.0 => "meteofrance_seamless",
                // Europe (extended coverage) - ICON provides excellent resolution
                >= 35.0 and <= 72.0 when lon is >= -15.0 and <= 45.0 => "icon_seamless",
                // Canada - GEM model
                >= 41.0 and <= 84.0 when lon is >= -141.0 and <= -52.0 => "gem_seamless",
                // Japan - JMA model
                >= 20.0 and <= 50.0 when lon is >= 120.0 and <= 150.0 => "jma_seamless",
                // Australia and New Zealand - BOM model
                >= -48.0 and <= -10.0 when lon is >= 110.0 and <= 180.0 => "bom_access_global",
                _ => "gfs_seamless"
            };
        }

        /// <summary>
        /// Determines the best Open-Meteo model for daily forecasts based on location.
        /// Selects models optimized for medium to long-range forecast accuracy.
        /// </summary>
        /// <param name="latitude">The latitude as a string.</param>
        /// <param name="longitude">The longitude as a string.</param>
        /// <returns>The model name to use for the API request.</returns>
        private static string GetBestDailyForecastModel(string latitude, string longitude)
        {
            if (!double.TryParse(latitude, out var lat) || !double.TryParse(longitude, out var lon))
                return "gfs_seamless"; // Global fallback

            // Use region-specific models for better accuracy in daily forecasts
            return lat switch
            {
                // Continental United States (CONUS) - NBM blends multiple models for superior accuracy
                >= 24.0 and <= 49.5 when lon is >= -125.0 and <= -66.0 => "ncep_nbm_conus",
                // United Kingdom and Ireland - UK Met Office
                >= 49.0 and <= 61.0 when lon is >= -11.0 and <= 3.0 => "ukmo_seamless",
                // France and nearby regions - Météo-France
                >= 41.0 and <= 52.0 when lon is >= -6.0 and <= 10.0 => "meteofrance_seamless",
                // Europe - ECMWF IFS is world-leading for medium-range forecasts
                >= 35.0 and <= 72.0 when lon is >= -15.0 and <= 45.0 => "ecmwf_ifs025",
                // Canada - GEM model
                >= 41.0 and <= 84.0 when lon is >= -141.0 and <= -52.0 => "gem_seamless",
                // Japan - JMA model
                >= 20.0 and <= 50.0 when lon is >= 120.0 and <= 150.0 => "jma_seamless",
                // Australia and New Zealand - BOM model
                >= -48.0 and <= -10.0 when lon is >= 110.0 and <= 180.0 => "bom_access_global",
                // Africa - GFS provides broad global coverage
                >= -35.0 and <= 38.0 when lon is >= -18.0 and <= 52.0 => "gfs_seamless",
                // Asia (excluding Japan) - GFS provides broad global coverage
                >= -10.0 and <= 55.0 when lon is >= 60.0 and <= 150.0 => "gfs_seamless",
                // South America - GFS for broad coverage
                >= -56.0 and <= 13.0 when lon is >= -82.0 and <= -34.0 => "gfs_seamless",
                _ => "gfs_seamless"
            };
        }

        #endregion

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
    [JsonSerializable(typeof(CurrentAirQualityResponse))]
    [JsonSerializable(typeof(HourlyAirQualityResponse))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    internal partial class OpenMeteoJsonContext : JsonSerializerContext { }
}