using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Xcalibur.Weather.Models.Services.OpenStreetMap;
using Xcalibur.Weather.Models.Services.OpenStreetMap.Response;

namespace Xcalibur.Weather.Services
{
    /// <summary>
    /// Service to interact with the OpenStreetMap Nominatim geocoding API.
    /// No API key is required. A descriptive User-Agent is mandatory per OSM policy.
    /// </summary>
    public class OpenStreetMapService
    {
        private readonly HttpClient _http;
        private readonly ILogger _logger;

        private const string OpenStreetMapUrl =
            "https://nominatim.openstreetmap.org/search?format=json&addressdetails=1&countrycodes={0}&q={1}";

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenStreetMapService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logger">The logger.</param>
        public OpenStreetMapService(HttpClient httpClient, ILogger logger)
        {
            _http = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Searches for locations matching <paramref name="query" />.
        /// </summary>
        /// <param name="query">Free-form address or place query.</param>
        /// <param name="country">The country.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A list of <see cref="OpenStreetMapResult" /> entries, or <c>null</c> on failure.
        /// </returns>
        public async Task<List<OpenStreetMapResultResponse>?> GetLocationsAsync(
            string query, string country, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(OpenStreetMapUrl, country, Uri.EscapeDataString(query));
                _logger.LogDebug("Fetching location data for query: '{Query}' in country: '{Country}'", query, country);


                // Create the HTTP request with the required User-Agent header
                using var request = ServiceHelper.CreateRequest(url);
                using var response = await _http.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Check if the response was successful
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenStreetMap API returned {StatusCode} for query: '{Query}' in country: '{Country}'",
                        response.StatusCode, query, country);
                    return null;
                }

                // Read the response stream and deserialize it
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                try
                {
                    return await JsonSerializer.DeserializeAsync(
                        stream,
                        OpenStreetMapJsonContext.Default.ListOpenStreetMapResultResponse,
                        cancellationToken);
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

    [JsonSerializable(typeof(List<OpenStreetMapResultResponse>))]
    [JsonSerializable(typeof(OpenStreetMapResultResponse))]
    [JsonSerializable(typeof(OpenStreetMapAddressResponse))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    internal partial class OpenStreetMapJsonContext : JsonSerializerContext { }
}
