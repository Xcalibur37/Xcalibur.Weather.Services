using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xcalibur.Weather.Models.Testing;

namespace Xcalibur.Weather.Services.Tests.Services
{
    /// <summary>
    /// Tests for <see cref="WeatherAlertService"/>.
    /// </summary>
    public sealed class WeatherAlertServiceTests
    {
        [Fact]
        public async Task GetMeteoalarmAlertsAsync_DeserializesResponse_WhenHttpOk()
        {
            // Arrange
            var json =
                """
                {
                  "alerts": [
                    {
                      "id": "alert-001",
                      "type": "Wind",
                      "event": "Strong Winds",
                      "headline": "Strong winds expected",
                      "onset": "2026-01-15T06:00:00Z",
                      "expires": "2026-01-16T06:00:00Z",
                      "severity": "Moderate"
                    }
                  ]
                }
                """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Act
            var result = await service.GetMeteoalarmAlertsAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Alerts.Should().HaveCount(1);
            result.Alerts![0].Id.Should().Be("alert-001");
            result.Alerts[0].Type.Should().Be("Wind");
        }

        [Fact]
        public async Task GetMeteoalarmAlertsAsync_ReturnsNull_OnNonSuccessStatus()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Act
            var result = await service.GetMeteoalarmAlertsAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetNwsAlertsAsync_ReturnsNull_OnNonSuccessStatus()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Act
            var result = await service.GetNwsAlertsAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetGdacsAlertsAsync_ReturnsNull_OnNonSuccessStatus()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Act
            var result = await service.GetGdacsAlertsAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetEnvironmentCanadaAlertsAsync_ReturnsNull_OnNonSuccessStatus()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Act
            var result = await service.GetEnvironmentCanadaAlertsAsync("43.65", "-79.38", "on", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetBomAlertsAsync_ReturnsNull_OnNonSuccessStatus()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadGateway);
            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Act
            var result = await service.GetBomAlertsAsync("-33.8688", "151.2093", "NSW", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetEmscAlertsAsync_ReturnsNull_OnNonSuccessStatus()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Act
            var result = await service.GetEmscAlertsAsync("39.43", "-77.80", 100, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetDwdAlertsAsync_ReturnsNull_OnNonSuccessStatus()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Act
            var result = await service.GetDwdAlertsAsync("48.1351", "11.5820", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetCombinedAlertsAsync_FetchesAllThreeSources()
        {
            // Arrange
            var meteoalarmJson = """{"alerts":[]}""";
            var nwsJson = """{"features":[]}""";
            var gdacsJson = """{"items":[]}""";

            var responses = new Queue<HttpResponseMessage>();
            responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(meteoalarmJson, Encoding.UTF8, "application/json")
            });
            responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(nwsJson, Encoding.UTF8, "application/json")
            });
            responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(gdacsJson, Encoding.UTF8, "application/json")
            });

            var handler = new QueuedResponseHandler(responses);
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Act
            var result = await service.GetCombinedAlertsAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Meteoalarm.Should().NotBeNull();
            result.Nws.Should().NotBeNull();
            result.Gdacs.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_AddsUserAgent_WhenMissing()
        {
            // Arrange
            using var http = new HttpClient();

            // Act
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Assert
            http.DefaultRequestHeaders.UserAgent.Should().NotBeEmpty();
            var userAgent = http.DefaultRequestHeaders.UserAgent.ToString();
            userAgent.Should().Contain("Xcalibur.Weather");
        }

        [Fact]
        public void Constructor_PreservesExistingUserAgent()
        {
            // Arrange
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "CustomAgent/1.0");

            // Act
            var service = new WeatherAlertService(http, NullLogger<WeatherAlertService>.Instance);

            // Assert
            var userAgent = http.DefaultRequestHeaders.GetValues("User-Agent").First();
            userAgent.Should().Be("CustomAgent/1.0");
        }

        private sealed class QueuedResponseHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;

            public QueuedResponseHandler(Queue<HttpResponseMessage> responses)
            {
                _responses = responses;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_responses.Dequeue());
            }
        }
    }
}
