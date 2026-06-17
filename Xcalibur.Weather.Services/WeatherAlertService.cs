using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Xcalibur.Weather.Models.Services.WeatherAlert.Bom;
using Xcalibur.Weather.Models.Services.WeatherAlert.Dwd;
using Xcalibur.Weather.Models.Services.WeatherAlert.Emsc;
using Xcalibur.Weather.Models.Services.WeatherAlert.EnvironmentCanada;
using Xcalibur.Weather.Models.Services.WeatherAlert.Gdacs;
using Xcalibur.Weather.Models.Services.WeatherAlert.Meteoalarm;
using Xcalibur.Weather.Models.Services.WeatherAlert.Nws;

namespace Xcalibur.Weather.Services
{
    /// <summary>
    /// Service to interact with weather alert APIs (Meteoalarm + NWS + GDACS + Environment Canada + BOM Australia + EMSC + DWD).
    /// </summary>
    public class WeatherAlertService
    {
        #region Fields

        private readonly HttpClient _http;
        private readonly ILogger _logger;

        private const string MeteoalarmUrl = "https://api.meteoalarm.org/v1/alerts?lat={0}&lon={1}";
        private const string NwsAlertsUrl = "https://api.weather.gov/alerts/active?point={0},{1}";
        private const string GdacsUrl = "https://www.gdacs.org/gdacsapi/api/events/geteventlist/MAP?lat={0}&lon={1}";
        private const string EnvironmentCanadaUrl = "https://weather.gc.ca/rss/warning/{0}_e.xml";
        private const string BomAlertsUrl = "http://www.bom.gov.au/fwo/{0}/warnings/{0}.warnings.json";
        private const string EmscUrl = "https://www.seismicportal.eu/fdsnws/event/1/query?format=json&limit=100&latitude={0}&longitude={1}&maxradiuskm={2}";
        private const string DwdAlertsUrl = "https://www.dwd.de/DWD/warnungen/warnapp/json/warnings.json";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherAlertService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logger">The logger.</param>
        public WeatherAlertService(HttpClient httpClient, ILogger logger)
        {
            _http = httpClient;
            _logger = logger;

            _http.DefaultRequestHeaders.ConnectionClose = false;

            // NWS requires a User-Agent header
            if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _http.DefaultRequestHeaders.Add("User-Agent", "Xcalibur.Weather/1.0 (weather-app; info@xcalibursystems.com)");
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets weather alerts from Meteoalarm API.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Meteoalarm response or null on error.</returns>
        public async Task<MeteoalarmResponse?> GetMeteoalarmAlertsAsync(
            string latitude, 
            string longitude, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(MeteoalarmUrl, latitude, longitude);
                _logger.LogDebug("Fetching Meteoalarm alerts for ({Latitude}, {Longitude})", latitude, longitude);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Meteoalarm API returned {StatusCode} for ({Latitude}, {Longitude})",
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync(stream,
                    WeatherAlertJsonContext.Default.MeteoalarmResponse, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching Meteoalarm alerts for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Meteoalarm request timed out or was cancelled for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize Meteoalarm response for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching Meteoalarm alerts for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
        }

        /// <summary>
        /// Gets weather alerts from NWS (National Weather Service) API.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>NWS alerts response or null on error.</returns>
        public async Task<NwsAlertsResponse?> GetNwsAlertsAsync(
            string latitude, 
            string longitude, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(NwsAlertsUrl, latitude, longitude);
                _logger.LogDebug("Fetching NWS alerts for ({Latitude}, {Longitude})", latitude, longitude);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("NWS API returned {StatusCode} for ({Latitude}, {Longitude})",
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync(stream,
                    WeatherAlertJsonContext.Default.NwsAlertsResponse, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching NWS alerts for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "NWS request timed out or was cancelled for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize NWS response for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching NWS alerts for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
        }

        /// <summary>
        /// Gets weather alerts from GDACS (Global Disaster Alert and Coordination System) API.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>GDACS response or null on error.</returns>
        public async Task<GdacsResponse?> GetGdacsAlertsAsync(
            string latitude,
            string longitude,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(GdacsUrl, latitude, longitude);
                _logger.LogDebug("Fetching GDACS alerts for ({Latitude}, {Longitude})", latitude, longitude);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GDACS API returned {StatusCode} for ({Latitude}, {Longitude})",
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync(stream,
                    WeatherAlertJsonContext.Default.GdacsResponse, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching GDACS alerts for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "GDACS request timed out or was cancelled for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize GDACS response for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching GDACS alerts for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
        }

        /// <summary>
        /// Gets weather alerts from Environment Canada for a specific location.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="provinceCode">The province/territory code (e.g., 'on' for Ontario, 'bc' for British Columbia). Should be derived from the address/coordinates.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Environment Canada response or null on error.</returns>
        public async Task<EnvironmentCanadaResponse?> GetEnvironmentCanadaAlertsAsync(
            string latitude,
            string longitude,
            string provinceCode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(EnvironmentCanadaUrl, provinceCode.ToLowerInvariant());
                _logger.LogDebug("Fetching Environment Canada alerts for ({Latitude}, {Longitude}) in province: {ProvinceCode}",
                    latitude, longitude, provinceCode);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Environment Canada API returned {StatusCode} for ({Latitude}, {Longitude}) in province {ProvinceCode}",
                        response.StatusCode, latitude, longitude, provinceCode);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var serializer = new XmlSerializer(typeof(EnvironmentCanadaResponse));
                return serializer.Deserialize(stream) as EnvironmentCanadaResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching Environment Canada alerts for ({Latitude}, {Longitude}) in {ProvinceCode}",
                    latitude, longitude, provinceCode);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Environment Canada request timed out or was cancelled for ({Latitude}, {Longitude}) in {ProvinceCode}",
                    latitude, longitude, provinceCode);
                return null;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to deserialize Environment Canada response for ({Latitude}, {Longitude}) in {ProvinceCode}",
                    latitude, longitude, provinceCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching Environment Canada alerts for ({Latitude}, {Longitude}) in {ProvinceCode}",
                    latitude, longitude, provinceCode);
                return null;
            }
        }

        /// <summary>
        /// Gets weather alerts from Australian Bureau of Meteorology for a specific location.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="stateCode">The state/territory code (e.g., 'nsw' for New South Wales, 'vic' for Victoria). Should be derived from the address/coordinates.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>BOM alerts response or null on error.</returns>
        public async Task<BomAlertsResponse?> GetBomAlertsAsync(
            string latitude,
            string longitude,
            string stateCode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = string.Format(BomAlertsUrl, stateCode.ToLowerInvariant());
                _logger.LogDebug("Fetching BOM alerts for ({Latitude}, {Longitude}) in state: {StateCode}",
                    latitude, longitude, stateCode);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("BOM API returned {StatusCode} for ({Latitude}, {Longitude}) in state {StateCode}",
                        response.StatusCode, latitude, longitude, stateCode);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync(stream,
                    WeatherAlertJsonContext.Default.BomAlertsResponse, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching BOM alerts for ({Latitude}, {Longitude}) in {StateCode}",
                    latitude, longitude, stateCode);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "BOM request timed out or was cancelled for ({Latitude}, {Longitude}) in {StateCode}",
                    latitude, longitude, stateCode);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize BOM response for ({Latitude}, {Longitude}) in {StateCode}",
                    latitude, longitude, stateCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching BOM alerts for ({Latitude}, {Longitude}) in {StateCode}",
                    latitude, longitude, stateCode);
                return null;
            }
        }

        #endregion

        #region EMSC Methods

        /// <summary>
        /// Gets earthquake alerts from EMSC (European-Mediterranean Seismological Centre) for a specific location.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="radiusKm">The search radius in kilometers (default: 500).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>EMSC response or null if request fails.</returns>
        public async Task<EmscResponse?> GetEmscAlertsAsync(
            string latitude,
            string longitude,
            int radiusKm = 500,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Fetching EMSC earthquake alerts for ({Latitude}, {Longitude}) within {Radius}km",
                latitude, longitude, radiusKm);

            try
            {
                var url = string.Format(EmscUrl, latitude, longitude, radiusKm);
                var response = await _http.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize(json, WeatherAlertJsonContext.Default.EmscResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching EMSC alerts for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
        }

        #endregion

        #region DWD Methods

        /// <summary>
        /// Gets weather warnings from DWD (Deutscher Wetterdienst - German Weather Service) for a specific location.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>DWD response or null if request fails.</returns>
        /// <remarks>
        /// Note: DWD API returns all warnings for Germany. Filtering by location should be done client-side using the provided coordinates.
        /// </remarks>
        public async Task<DwdAlertsResponse?> GetDwdAlertsAsync(
            string latitude,
            string longitude,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Fetching DWD weather warnings for ({Latitude}, {Longitude})",
                latitude, longitude);

            try
            {
                var response = await _http.GetAsync(DwdAlertsUrl, cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize(json, WeatherAlertJsonContext.Default.DwdAlertsResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching DWD alerts for ({Latitude}, {Longitude})",
                    latitude, longitude);
                return null;
            }
        }

        #endregion

        #region Combined Methods

        /// <summary>
        /// Gets combined weather alerts from Meteoalarm, NWS, and GDACS.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Tuple containing Meteoalarm, NWS, and GDACS responses.</returns>
        public async Task<(MeteoalarmResponse? Meteoalarm, NwsAlertsResponse? Nws, GdacsResponse? Gdacs)> GetCombinedAlertsAsync(
            string latitude,
            string longitude,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Fetching combined alerts from Meteoalarm, NWS, and GDACS for ({Latitude}, {Longitude})",
                latitude, longitude);

            var meteoalarmTask = GetMeteoalarmAlertsAsync(latitude, longitude, cancellationToken);
            var nwsTask = GetNwsAlertsAsync(latitude, longitude, cancellationToken);
            var gdacsTask = GetGdacsAlertsAsync(latitude, longitude, cancellationToken);

            await Task.WhenAll(meteoalarmTask, nwsTask, gdacsTask);

            return (await meteoalarmTask, await nwsTask, await gdacsTask);
        }

        #endregion
    }

    /// <summary>
    /// JSON serialization context for weather alert types.
    /// </summary>
    [JsonSerializable(typeof(MeteoalarmResponse))]
    [JsonSerializable(typeof(MeteoalarmAlertResponse))]
    [JsonSerializable(typeof(List<MeteoalarmAlertResponse>))]
    [JsonSerializable(typeof(NwsAlertsResponse))]
    [JsonSerializable(typeof(NwsAlertResponse))]
    [JsonSerializable(typeof(NwsAlertPropertiesResponse))]
    [JsonSerializable(typeof(NwsGeocodeResponse))]
    [JsonSerializable(typeof(List<NwsAlertResponse>))]
    [JsonSerializable(typeof(GdacsResponse))]
    [JsonSerializable(typeof(GdacsFeatureResponse))]
    [JsonSerializable(typeof(GdacsEventResponse))]
    [JsonSerializable(typeof(GdacsGeometryResponse))]
    [JsonSerializable(typeof(List<GdacsFeatureResponse>))]
    [JsonSerializable(typeof(BomAlertsResponse))]
    [JsonSerializable(typeof(BomMetadataResponse))]
    [JsonSerializable(typeof(BomWarningResponse))]
    [JsonSerializable(typeof(List<BomWarningResponse>))]
    [JsonSerializable(typeof(EmscResponse))]
    [JsonSerializable(typeof(EmscFeatureResponse))]
    [JsonSerializable(typeof(EmscEventResponse))]
    [JsonSerializable(typeof(EmscGeometryResponse))]
    [JsonSerializable(typeof(List<EmscFeatureResponse>))]
    [JsonSerializable(typeof(DwdAlertsResponse))]
    [JsonSerializable(typeof(DwdWarningResponse))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    internal partial class WeatherAlertJsonContext : JsonSerializerContext
    {
    }
}
