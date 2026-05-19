using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Xcalibur.Weather.Models.WeatherProvider.GoogleWeatherAlerts;

namespace Xcalibur.Weather.Services.WeatherProvider.GoogleWeatherAlerts
{
    /// <summary>
    /// Service to interact with the Google Weather Alerts API.
    /// </summary>
    public class GoogleWeatherAlertsService
    {
        private readonly HttpClient _http;
        private readonly string _token;
        private readonly ILogger<GoogleWeatherAlertsService> _logger;

        private const string TestUrl = "https://weather.googleapis.com/v1/publicAlerts:lookup?key={0}&location.longitude=-77.804161&location.latitude=39.4300996";
        private const string AlertsUrl = "https://weather.googleapis.com/v1/publicAlerts:lookup?key={0}&location.longitude={1}&location.latitude={2}";

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleWeatherAlertsService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="token">The token.</param>
        /// <param name="logger">The logger.</param>
        public GoogleWeatherAlertsService(HttpClient httpClient, string token, ILogger<GoogleWeatherAlertsService> logger)
        {
            _http = httpClient;
            _token = token;
            _logger = logger;

            _http.DefaultRequestHeaders.ConnectionClose = false;
        }

        /// <summary>
        /// Tests the API key.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<bool> TestApiKey(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(TestUrl, _token);
                _logger.LogDebug("Testing API Key");

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("API key test failed");
                    return false;
                }

                _logger.LogDebug("API key test succeeded");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error testing API key");
                return false;
            }
        }

        /// <summary>
        /// Gets the weather alerts for the specified coordinates.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<WeatherAlertsResponse?> GetWeatherAlertsAsync(string latitude, string longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(AlertsUrl, _token, longitude, latitude);
                _logger.LogDebug("Fetching weather alerts for ({Latitude}, {Longitude})", latitude, longitude);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Google Weather Alerts API returned {StatusCode} for ({Latitude}, {Longitude})",
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                try
                {
                    return await JsonSerializer.DeserializeAsync(stream,
                        GoogleWeatherAlertsJsonContext.Default.WeatherAlertsResponse, cancellationToken);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize Google Weather Alerts response for ({Latitude}, {Longitude})",
                        latitude, longitude);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching weather alerts for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Weather alerts request timed out or was cancelled for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching weather alerts for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
        }
    }

    [JsonSerializable(typeof(WeatherAlertsResponse))]
    [JsonSerializable(typeof(WeatherAlertModel))]
    [JsonSerializable(typeof(AlertTitleModel))]
    [JsonSerializable(typeof(AlertDataSourceModel))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    internal partial class GoogleWeatherAlertsJsonContext : JsonSerializerContext
    {
    }
}
