using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Xcalibur.Extensions.V2;
using Xcalibur.Weather.Models.Services.Atmospore.Response;

namespace Xcalibur.Weather.Services
{
    /// <summary>
    /// Service to interact with the Atmospore pollen API.
    /// </summary>
    public class AtmosporeService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly ILogger<AtmosporeService> _logger;

        private const string BaseUrl = "https://pollenapi.com/v1/pollen";
        private const string TestUrl = BaseUrl + "?lon=-77.804161&lat=39.4300996&dt=2026-05-27&forecast_days=1";
        private const string ForecastUrlTemplate = BaseUrl + "?lon={0}&lat={1}&dt={2}&forecast_days={3}";
        private Dictionary<string, string> _headers = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="AtmosporeService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="logger">The logger.</param>
        public AtmosporeService(HttpClient httpClient, string apiKey, ILogger<AtmosporeService> logger)
        {
            _http = httpClient;
            _apiKey = apiKey;
            _logger = logger;

            _http.DefaultRequestHeaders.ConnectionClose = false;

            // Headers
            BuildHeaders(_apiKey);
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
                _logger.LogDebug("Testing Atmospore API key");

                // The Atmospore API uses the x-api-key header for authentication, so we need to include that in our test request
                using var request = new HttpRequestMessage(HttpMethod.Get, TestUrl);

                // Set headers for the request
                SetHeaders(request);

                // Call service
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Check for non-success status codes (e.g., 401 Unauthorized, 403 Forbidden)
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("Atmospore API key test failed with status {StatusCode}", response.StatusCode);
                    return false;
                }

                // Successful response indicates valid API key
                _logger.LogDebug("Atmospore API key test succeeded");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error testing Atmospore API key");
                return false;
            }
        }

        /// <summary>
        /// Gets the pollen forecast for the specified coordinates and date.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="date">The forecast date (yyyy-MM-dd). Defaults to today if null or empty.</param>
        /// <param name="forecastDays">The number of forecast days.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<PollenResponse?> GetPollenForecastAsync(string latitude, string longitude,
            string? date = null, int forecastDays = 1, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input parameters
                var dt = string.IsNullOrWhiteSpace(date)
                    ? DateTime.Today.ToString("yyyy-MM-dd")
                    : date;

                // Construct the API URL with query parameters
                var url = string.Format(ForecastUrlTemplate, longitude, latitude, dt, forecastDays);
                _logger.LogDebug("Fetching Atmospore pollen forecast for ({Latitude}, {Longitude}) on {Date}", latitude, longitude, dt);

                // The Atmospore API uses the x-api-key header for authentication, so we need to include that in our request
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                // Set headers for the request
                SetHeaders(request);

                // Call service
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Check for non-success status codes
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Atmospore API returned {StatusCode} for forecast at ({Latitude}, {Longitude})",
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                // Read and deserialize the response stream
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                try
                {
                    return await JsonSerializer.DeserializeAsync(stream,
                        AtmosporeJsonContext.Default.PollenResponse, cancellationToken);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize Atmospore pollen response for ({Latitude}, {Longitude})",
                        latitude, longitude);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching Atmospore pollen forecast for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Atmospore pollen forecast request timed out or was cancelled for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching Atmospore pollen forecast for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
        }

        /// <summary>
        /// Builds the headers.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        private void BuildHeaders(string apiKey)
        {
            _headers = new Dictionary<string, string>()
            {
                {"x-api-key", _apiKey},
                {"accept", "application/json"}
            };
        }

        /// <summary>
        /// Sets the headers.
        /// </summary>
        /// <param name="request">The request.</param>
        private void SetHeaders(HttpRequestMessage? request)
        {
            if (request == null) return;
            _headers.Apply(x => request.Headers.Add(x.Key, x.Value));
        }
    }

    [JsonSerializable(typeof(PollenResponse))]
    [JsonSerializable(typeof(PollenMetaResponse))]
    [JsonSerializable(typeof(PollenLocationResponse))]
    [JsonSerializable(typeof(PollenEntryResponse))]
    [JsonSerializable(typeof(List<PollenEntryResponse>))]
    [JsonSerializable(typeof(PollenSpeciesEntryResponse))]
    [JsonSerializable(typeof(Dictionary<string, PollenSpeciesEntryResponse>))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    internal partial class AtmosporeJsonContext : JsonSerializerContext
    {
    }
}
