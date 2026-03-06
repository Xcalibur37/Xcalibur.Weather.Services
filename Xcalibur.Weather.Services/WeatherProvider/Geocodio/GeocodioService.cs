using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Xcalibur.Weather.Models.WeatherProvider.Geocodio;

namespace Xcalibur.Weather.Services.WeatherProvider.Geocodio
{
    /// <summary>
    /// Service to interact with the Geocodio Geocoding API.
    /// </summary>
    public class GeocodioService
    {
        private readonly HttpClient _http;
        private readonly string _token;
        private readonly ILogger _logger;

        // Use source-generated context for AOT and trimming safety
        private const string TestUrl = "https://api.geocod.io/v1.9/geocode?api_key={0}";
        private const string GeocodioUrl = "https://api.geocod.io/v1.9/geocode?q={0}&country={1}&api_key={2}";

        /// <summary>
        /// Initializes a new instance of the <see cref="GeocodioService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="token">The token.</param>
        /// <param name="logger">The logger.</param>
        public GeocodioService(HttpClient httpClient, string token, ILogger logger)
        {
            _http = httpClient;
            _token = token;
            _logger = logger;

            // Enable SSL for AOT
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

                // Check for non-success status codes (e.g., 401 Unauthorized, 403 Forbidden)
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("API key test failed");
                    return false;
                }

                // Successful response indicates valid API key
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
        /// Gets the locations asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="country">The country.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<GeocodioResponse?> GetLocationsAsync(string query, string country, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(GeocodioUrl, query, country, _token);
                _logger.LogDebug("Fetching location data for query: '{Query}' in country: '{Country}'", query, country);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Geocodio API returned {StatusCode} for query: '{Query}' in country: '{Country}'",
                        response.StatusCode, query, country);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                // Simple streaming deserialize
                try
                {
                    return await JsonSerializer.DeserializeAsync(stream,
                        GeocodioJsonContext.Default.GeocodioResponse, cancellationToken);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize Geocodio response for query: '{Query}' in country: '{Country}'",
                        query, country);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching location data for query: '{Query}' in country: '{Country}'",
                    query, country);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Location data request timed out or was cancelled for query: '{Query}' in country: '{Country}'",
                    query, country);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching location data for query: '{Query}' in country: '{Country}'",
                    query, country);
                return null;
            }
        }
    }

    // Add a source generation context for System.Text.Json
    [JsonSerializable(typeof(GeocodioResponse))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    internal partial class GeocodioJsonContext : JsonSerializerContext
    {
    }
}