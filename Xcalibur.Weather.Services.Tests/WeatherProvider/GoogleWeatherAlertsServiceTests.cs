using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xcalibur.Weather.Models.Testing;
using Xcalibur.Weather.Services.WeatherProvider.GoogleWeatherAlerts;

namespace Xcalibur.Weather.Services.Tests.WeatherProvider
{
    /// <summary>
    /// Tests for <see cref="GoogleWeatherAlertsService"/>.
    /// </summary>
    public sealed class GoogleWeatherAlertsServiceTests
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
            var service = new GoogleWeatherAlertsService(http, "DUMMY_KEY", NullLogger<GoogleWeatherAlertsService>.Instance);

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
            var service = new GoogleWeatherAlertsService(http, "BAD_KEY", NullLogger<GoogleWeatherAlertsService>.Instance);

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
            var service = new GoogleWeatherAlertsService(http, "BAD_KEY", NullLogger<GoogleWeatherAlertsService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetWeatherAlertsAsync_DeserializesResponse_WhenHttpOk()
        {
            // Arrange
            var json =
                """
                {
                  "regionCode": "US-MD",
                  "weatherAlerts": [
                    {
                      "alertId": "alert-001",
                      "alertTitle": {
                        "text": "Winter Storm Warning",
                        "languageCode": "en"
                      },
                      "eventType": "Winter Storm",
                      "areaName": "Frederick County",
                      "description": "Heavy snow expected.",
                      "severity": "Extreme",
                      "certainty": "Likely",
                      "urgency": "Immediate",
                      "instruction": ["Stay indoors.", "Avoid travel."],
                      "timezoneOffset": "-18000s",
                      "startTime": "2024-01-15T06:00:00Z",
                      "expirationTime": "2024-01-16T06:00:00Z",
                      "dataSource": {
                        "publisher": "NOAA",
                        "name": "National Weather Service",
                        "authorityUri": "https://www.weather.gov"
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
            var service = new GoogleWeatherAlertsService(http, "DUMMY_KEY", NullLogger<GoogleWeatherAlertsService>.Instance);

            // Act
            var result = await service.GetWeatherAlertsAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.RegionCode.Should().Be("US-MD");
            result.WeatherAlerts.Should().HaveCount(1);

            var alert = result.WeatherAlerts![0];
            alert.AlertId.Should().Be("alert-001");
            alert.AlertTitle.Should().NotBeNull();
            alert.AlertTitle!.Text.Should().Be("Winter Storm Warning");
            alert.AlertTitle.LanguageCode.Should().Be("en");
            alert.EventType.Should().Be("Winter Storm");
            alert.AreaName.Should().Be("Frederick County");
            alert.Severity.Should().Be("Extreme");
            alert.Certainty.Should().Be("Likely");
            alert.Urgency.Should().Be("Immediate");
            alert.Instruction.Should().HaveCount(2);
            alert.StartTime.Should().Be("2024-01-15T06:00:00Z");
            alert.ExpirationTime.Should().Be("2024-01-16T06:00:00Z");
            alert.DataSource.Should().NotBeNull();
            alert.DataSource!.Publisher.Should().Be("NOAA");
            alert.DataSource.Name.Should().Be("National Weather Service");
            alert.DataSource.AuthorityUri.Should().Be("https://www.weather.gov");
        }

        [Fact]
        public async Task GetWeatherAlertsAsync_DeserializesEmptyAlerts_WhenNoAlertsActive()
        {
            // Arrange
            var json =
                """
                {
                  "regionCode": "US-MD",
                  "weatherAlerts": []
                }
                """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new GoogleWeatherAlertsService(http, "DUMMY_KEY", NullLogger<GoogleWeatherAlertsService>.Instance);

            // Act
            var result = await service.GetWeatherAlertsAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.RegionCode.Should().Be("US-MD");
            result.WeatherAlerts.Should().BeEmpty();
        }

        [Fact]
        public async Task GetWeatherAlertsAsync_BuildsCorrectUrl()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"regionCode":"US-MD","weatherAlerts":[]}""", Encoding.UTF8, "application/json")
            };

            var handler = new RecordingHandler(response);
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new GoogleWeatherAlertsService(http, "DUMMY_KEY", NullLogger<GoogleWeatherAlertsService>.Instance);

            // Act
            await service.GetWeatherAlertsAsync("39.4300996", "-77.804161", CancellationToken.None);

            // Assert
            handler.Request.Should().NotBeNull();
            var requestUri = handler.Request!.RequestUri!.ToString();
            requestUri.Should().Be(
                "https://weather.googleapis.com/v1/publicAlerts:lookup?key=DUMMY_KEY&location.longitude=-77.804161&location.latitude=39.4300996");
        }

        [Fact]
        public async Task GetWeatherAlertsAsync_ReturnsNull_OnNonSuccessStatus()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("bad") };
            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new GoogleWeatherAlertsService(http, "DUMMY_KEY", NullLogger<GoogleWeatherAlertsService>.Instance);

            // Act
            var result = await service.GetWeatherAlertsAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetWeatherAlertsAsync_ReturnsNull_WhenResponseIsInvalidJson()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid-json ", Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);
            var service = new GoogleWeatherAlertsService(http, "DUMMY_KEY", NullLogger<GoogleWeatherAlertsService>.Instance);

            // Act
            var result = await service.GetWeatherAlertsAsync("39.43", "-77.80", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }
    }
}
