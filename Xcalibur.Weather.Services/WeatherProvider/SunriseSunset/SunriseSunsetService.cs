using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Xcalibur.Weather.Models.WeatherProvider.SunriseSunset;

namespace Xcalibur.Weather.Services.WeatherProvider.SunriseSunset
{
    /// <summary>
    /// Service to interact with the SunriseSunset.io JSON API.
    /// No API key required; accepts latitude and longitude.
    /// </summary>
    public class SunriseSunsetService
    {
        private readonly HttpClient _http;
        private readonly ILogger<SunriseSunsetService> _logger;

        private const string AstronomyUrl = "https://api.sunrisesunset.io/json?lat={0}&lng={1}";

        /// <summary>
        /// Initializes a new instance of the <see cref="SunriseSunsetService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logger">The logger.</param>
        public SunriseSunsetService(HttpClient httpClient, ILogger<SunriseSunsetService> logger)
        {
            _http = httpClient;
            _logger = logger;

            _http.DefaultRequestHeaders.ConnectionClose = false;
        }

        /// <summary>
        /// Gets the sun and moon data for the given coordinates.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A <see cref="SunriseSunsetResponse"/> on success; <c>null</c> on failure.
        /// </returns>
        public async Task<SunriseSunsetResponse?> GetSunriseSunsetAsync(string latitude, string longitude,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(AstronomyUrl, latitude, longitude);
                _logger.LogDebug("Fetching sunrise/sunset data for ({Latitude}, {Longitude})", latitude, longitude);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("SunriseSunset API returned {StatusCode} for ({Latitude}, {Longitude})",
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                try
                {
                    return await JsonSerializer.DeserializeAsync(stream,
                        SunriseSunsetJsonContext.Default.SunriseSunsetResponse, cancellationToken);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize SunriseSunset response for ({Latitude}, {Longitude})",
                        latitude, longitude);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching sunrise/sunset data for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Sunrise/sunset data request timed out or was cancelled for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching sunrise/sunset data for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
        }
    }

    /// <summary>
    /// Source-generated JSON serializer context for <see cref="SunriseSunsetResponse"/>.
    /// </summary>
    [JsonSerializable(typeof(SunriseSunsetResponse))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    internal partial class SunriseSunsetJsonContext : JsonSerializerContext
    {
    }
}
