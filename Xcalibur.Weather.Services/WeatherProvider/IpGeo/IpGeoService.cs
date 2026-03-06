using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Xcalibur.Weather.Models.WeatherProvider.IpGeo.Astronomy;

namespace Xcalibur.Weather.Services.WeatherProvider.IpGeo
{
    /// <summary>
    /// Service to interact with the IpGeo Astronomy API.
    /// </summary>
    public class IpGeoService
    {
        private readonly HttpClient _http;
        private readonly string _token;
        private readonly ILogger<IpGeoService> _logger;

        // Use source-generated context for AOT and trimming safety
        private const string TestUrl = "https://api.ipgeolocation.io/v2/astronomy?apiKey={0}";
        private const string AstronomyUrl = "https://api.ipgeolocation.io/v2/astronomy?apiKey={0}&lat={1}&long={2}";

        /// <summary>
        /// Initializes a new instance of the <see cref="IpGeoService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="token">The token.</param>
        /// <param name="logger">The logger.</param>
        public IpGeoService(HttpClient httpClient, string token, ILogger<IpGeoService> logger)
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
                if (response.StatusCode == HttpStatusCode.Unauthorized)
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
        /// Gets the sun and moon data from the Astronomy API.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<SunMoonDataResponse?> GetSunMoonDataAsync(string latitude, string longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(AstronomyUrl, _token, latitude, longitude);
                _logger.LogDebug("Fetching sun/moon data for ({Latitude}, {Longitude})", latitude, longitude);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("IpGeo API returned {StatusCode} for sun/moon data at ({Latitude}, {Longitude})",
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                // Simple streaming deserialize
                try
                {
                    return await JsonSerializer.DeserializeAsync(stream,
                        IpGeoJsonContext.Default.SunMoonDataResponse, cancellationToken);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize IpGeo sun/moon response for ({Latitude}, {Longitude})",
                        latitude, longitude);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching sun/moon data for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Sun/moon data request timed out or was cancelled for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching sun/moon data for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
        }
    }

    // Add a source generation context for System.Text.Json
    [JsonSerializable(typeof(SunMoonDataResponse))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    internal partial class IpGeoJsonContext : JsonSerializerContext
    {
    }
}