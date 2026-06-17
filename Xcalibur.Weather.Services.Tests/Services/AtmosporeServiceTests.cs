using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xcalibur.Weather.Models.Testing;

namespace Xcalibur.Weather.Services.Tests.Services
{
    /// <summary>
    /// Tests for <see cref="AtmosporeService"/>.
    /// </summary>
    public sealed class AtmosporeServiceTests
    {
        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public RecordingHandler(HttpResponseMessage response)
            {
                _response = response;
            }

            public HttpRequestMessage? Request { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Request = request;
                return Task.FromResult(_response);
            }
        }

        [Fact]
        public async Task TestApiKey_ReturnsTrue_WhenHttpOk()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new AtmosporeService(http, "DUMMY_KEY", NullLogger<AtmosporeService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task TestApiKey_ReturnsFalse_WhenUnauthorized()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new AtmosporeService(http, "BAD_KEY", NullLogger<AtmosporeService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TestApiKey_ReturnsFalse_WhenForbidden()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new AtmosporeService(http, "BAD_KEY", NullLogger<AtmosporeService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetPollenForecastAsync_DeserializesResponse_WhenHttpOk()
        {
            // Arrange
            var json =
                """
                {
                  "meta": {
                    "location": {
                      "lat": 39.43,
                      "lon": -77.8
                    },
                    "units": "grains/m³",
                    "generated_at": "2026-05-27T00:00:00Z"
                  },
                  "data": [
                    {
                      "date": "2026-05-27",
                      "overall_risk": "moderate",
                      "species": {
                        "Grass": {
                          "value": 45.2,
                          "risk_level": "moderate",
                          "display_name": "Grass",
                          "category": "grass"
                        },
                        "Tree": {
                          "value": 12.1,
                          "risk_level": "low",
                          "display_name": "Tree",
                          "category": "tree"
                        }
                      }
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
            var service = new AtmosporeService(http, "DUMMY_KEY", NullLogger<AtmosporeService>.Instance);

            // Act
            var result = await service.GetPollenForecastAsync("39.43", "-77.80", "2026-05-27", 1, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Meta.Should().NotBeNull();
            result.Meta!.Location.Should().NotBeNull();
            result.Meta.Location!.Lat.Should().Be(39.43);
            result.Meta.Location.Lon.Should().Be(-77.8);
            result.Meta.Units.Should().Be("grains/m³");
            result.Data.Should().HaveCount(1);
            result.Data![0].Date.Should().Be("2026-05-27");
            result.Data[0].OverallRisk.Should().Be("moderate");
            result.Data[0].Species.Should().ContainKey("Grass");
            result.Data[0].Species!["Grass"].Value.Should().Be(45.2);
            result.Data[0].Species["Grass"].RiskLevel.Should().Be("moderate");
            result.Data[0].Species["Grass"].DisplayName.Should().Be("Grass");
        }

        [Fact]
        public async Task GetPollenForecastAsync_BuildsCorrectUrl_WithExplicitDate()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"meta":{},"location":{},"data":[]}""", Encoding.UTF8, "application/json")
            };

            var handler = new RecordingHandler(response);
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new AtmosporeService(http, "DUMMY_KEY", NullLogger<AtmosporeService>.Instance);

            // Act
            await service.GetPollenForecastAsync("39.4300996", "-77.804161", "2026-05-27", 3, CancellationToken.None);

            // Assert
            handler.Request.Should().NotBeNull();
            var requestUri = handler.Request!.RequestUri!.ToString();
            requestUri.Should().Be("https://pollenapi.com/v1/pollen?lon=-77.804161&lat=39.4300996&dt=2026-05-27&forecast_days=3");
        }

        [Fact]
        public async Task GetPollenForecastAsync_UsesTodayDate_WhenDateIsNull()
        {
            // Arrange
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"meta":{},"location":{},"data":[]}""", Encoding.UTF8, "application/json")
            };

            var handler = new RecordingHandler(response);
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new AtmosporeService(http, "DUMMY_KEY", NullLogger<AtmosporeService>.Instance);

            // Act
            await service.GetPollenForecastAsync("39.43", "-77.80", null, 1, CancellationToken.None);

            // Assert
            handler.Request.Should().NotBeNull();
            var requestUri = handler.Request!.RequestUri!.ToString();
            requestUri.Should().Contain($"dt={today}");
        }

        [Fact]
        public async Task GetPollenForecastAsync_IncludesApiKeyHeader()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"meta":{},"location":{},"data":[]}""", Encoding.UTF8, "application/json")
            };

            var handler = new RecordingHandler(response);
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new AtmosporeService(http, "TEST_API_KEY_123", NullLogger<AtmosporeService>.Instance);

            // Act
            await service.GetPollenForecastAsync("39.43", "-77.80", "2026-05-27", 1, CancellationToken.None);

            // Assert
            handler.Request.Should().NotBeNull();
            handler.Request!.Headers.Should().Contain(h => h.Key == "x-api-key");
            var apiKeyHeader = handler.Request.Headers.GetValues("x-api-key").First();
            apiKeyHeader.Should().Be("TEST_API_KEY_123");
        }

        [Fact]
        public async Task GetPollenForecastAsync_ReturnsNull_OnNonSuccessStatus()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("bad") };
            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new AtmosporeService(http, "DUMMY_KEY", NullLogger<AtmosporeService>.Instance);

            // Act
            var result = await service.GetPollenForecastAsync("39.43", "-77.80", "2026-05-27", 1, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetPollenForecastAsync_ReturnsNull_WhenResponseIsInvalidJson()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid-json ", Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new AtmosporeService(http, "DUMMY_KEY", NullLogger<AtmosporeService>.Instance);

            // Act
            var result = await service.GetPollenForecastAsync("39.43", "-77.80", "2026-05-27", 1, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }
    }
}
