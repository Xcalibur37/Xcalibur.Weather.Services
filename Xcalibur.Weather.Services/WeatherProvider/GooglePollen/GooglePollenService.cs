using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Xcalibur.Weather.Models.WeatherProvider.GooglePollen.Forecast;

namespace Xcalibur.Weather.Services.WeatherProvider.GooglePollen
{
    /// <summary>
    /// Service to interact with the Google Pollen forecast API.
    /// </summary>
    public class GooglePollenService
    {
        private readonly HttpClient _http;
        private readonly string _token;
        private readonly ILogger<GooglePollenService> _logger;

        // Use source-generated context for AOT and trimming safety
        private const string TestUrl = "https://pollen.googleapis.com/v1/forecast:lookup?key={0}&location.longitude=-77.804161&location.latitude=39.4300996&days=1";
        private const string ForecastUrl = "https://pollen.googleapis.com/v1/forecast:lookup?key={0}&location.longitude={1}&location.latitude={2}&days=1";

        /// <summary>
        /// Initializes a new instance of the <see cref="GooglePollenService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="token">The token.</param>
        /// <param name="logger">The logger.</param>
        public GooglePollenService(HttpClient httpClient, string token, ILogger<GooglePollenService> logger)
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
        /// Gets the pollen forecast data.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<PollenForecastResponse?> GetPollenForecastAsync(string latitude, string longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(ForecastUrl, _token, longitude, latitude);
                _logger.LogDebug("Fetching pollen forecast for ({Latitude}, {Longitude})", latitude, longitude);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Google Pollen API returned {StatusCode} for forecast at ({Latitude}, {Longitude})",
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                try
                {
                    return await JsonSerializer.DeserializeAsync(stream,
                        GooglePollenJsonContext.Default.PollenForecastResponse, cancellationToken);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize Google Pollen forecast response for ({Latitude}, {Longitude})",
                        latitude, longitude);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching pollen forecast for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Pollen forecast request timed out or was cancelled for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching pollen forecast for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
        }
    }

    [JsonSerializable(typeof(PollenForecastResponse))]
    [JsonSerializable(typeof(DailyInfoModel))]
    [JsonSerializable(typeof(ForecastDateModel))]
    [JsonSerializable(typeof(PollenTypeInfoModel))]
    [JsonSerializable(typeof(PlantInfoModel))]
    [JsonSerializable(typeof(IndexInfoModel))]
    [JsonSerializable(typeof(ColorModel))]
    [JsonSerializable(typeof(PlantDescriptionModel))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    internal partial class GooglePollenJsonContext : JsonSerializerContext
    {
    }
}
