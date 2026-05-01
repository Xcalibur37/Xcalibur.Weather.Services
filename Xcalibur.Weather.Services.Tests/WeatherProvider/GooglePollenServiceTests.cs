using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xcalibur.Weather.Models.Testing;
using Xcalibur.Weather.Services.WeatherProvider.GooglePollen;

namespace Xcalibur.Weather.Services.Tests.WeatherProvider
{
    /// <summary>
    /// Tests for <see cref="GooglePollenService"/>.
    /// </summary>
    public sealed class GooglePollenServiceTests
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

            var service = new GooglePollenService(http, "DUMMY_KEY", NullLogger<GooglePollenService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task TestApiKey_ReturnsFalse_WhenUnauthorized()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("unauthorized")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new GooglePollenService(http, "DUMMY_KEY", NullLogger<GooglePollenService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TestApiKey_ReturnsFalse_WhenForbidden()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("forbidden")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new GooglePollenService(http, "DUMMY_KEY", NullLogger<GooglePollenService>.Instance);

            // Act
            var result = await service.TestApiKey(CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetPollenForecastAsync_ReturnsDeserializedResponse_WhenHttpOk()
        {
            // Arrange
            var json =
                """
                {
                  "regionCode": "US",
                  "dailyInfo": [
                    {
                      "date": { "year": 2026, "month": 5, "day": 1 },
                      "pollenTypeInfo": [
                        {
                          "code": "TREE",
                          "displayName": "Tree",
                          "inSeason": true,
                          "indexInfo": {
                            "code": "UPI",
                            "displayName": "Universal Pollen Index",
                            "value": 2,
                            "category": "Low",
                            "indexDescription": "People with high allergy to pollen are likely to experience symptoms",
                            "color": { "red": 0.5176471, "green": 0.8117647, "blue": 0.2 }
                          },
                          "healthRecommendations": [
                            "It's a good day for outdoor activities since pollen levels are low."
                          ]
                        }
                      ],
                      "plantInfo": [
                        {
                          "code": "MAPLE",
                          "displayName": "Maple",
                          "inSeason": false,
                          "indexInfo": {
                            "code": "UPI",
                            "displayName": "Universal Pollen Index",
                            "value": 2,
                            "category": "Low",
                            "indexDescription": "People with high allergy to pollen are likely to experience symptoms",
                            "color": { "red": 0.5176471, "green": 0.8117647, "blue": 0.2 }
                          },
                          "plantDescription": {
                            "type": "TREE",
                            "family": "Sapindaceae",
                            "season": "Spring",
                            "specialColors": "Golden and red leaves",
                            "specialShapes": "Palmate leaves",
                            "crossReaction": "Plane tree pollen.",
                            "picture": "https://example.com/maple.jpg",
                            "pictureCloseup": "https://example.com/maple-close.jpg"
                          }
                        }
                      ]
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

            var service = new GooglePollenService(http, "DUMMY_KEY", NullLogger<GooglePollenService>.Instance);

            // Act
            var result = await service.GetPollenForecastAsync("39.4300996", "-77.804161", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            var forecast = result!;
            forecast.RegionCode.Should().Be("US");
            forecast.DailyInfo.Should().NotBeNull();
            forecast.DailyInfo.Should().HaveCount(1);

            var dailyInfo = forecast.DailyInfo![0];
            dailyInfo.Date.Should().NotBeNull();
            dailyInfo.Date!.Year.Should().Be(2026);
            dailyInfo.PollenTypeInfo.Should().NotBeNull();
            dailyInfo.PollenTypeInfo.Should().HaveCount(1);

            var pollenType = dailyInfo.PollenTypeInfo![0];
            pollenType.Code.Should().Be("TREE");
            pollenType.IndexInfo.Should().NotBeNull();
            pollenType.IndexInfo!.Value.Should().Be(2);

            dailyInfo.PlantInfo.Should().NotBeNull();
            dailyInfo.PlantInfo.Should().HaveCount(1);

            var plant = dailyInfo.PlantInfo![0];
            plant.PlantDescription.Should().NotBeNull();
            plant.PlantDescription!.Type.Should().Be("TREE");
        }

        [Fact]
        public async Task GetPollenForecastAsync_UsesExpectedRequestUrl()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"regionCode\":\"US\",\"dailyInfo\":[]}", Encoding.UTF8, "application/json")
            };

            var handler = new RecordingHandler(response);
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new GooglePollenService(http, "DUMMY_KEY", NullLogger<GooglePollenService>.Instance);

            // Act
            await service.GetPollenForecastAsync("39.4300996", "-77.804161", CancellationToken.None);

            // Assert
            handler.Request.Should().NotBeNull();
            handler.Request!.Method.Should().Be(HttpMethod.Get);
            handler.Request.RequestUri.Should().NotBeNull();
            handler.Request.RequestUri!.ToString().Should().Be("https://pollen.googleapis.com/v1/forecast:lookup?key=DUMMY_KEY&location.longitude=-77.804161&location.latitude=39.4300996&days=1");
        }

        [Fact]
        public async Task GetPollenForecastAsync_ReturnsNull_WhenStatusNotSuccess()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new GooglePollenService(http, "DUMMY_KEY", NullLogger<GooglePollenService>.Instance);

            // Act
            var result = await service.GetPollenForecastAsync("39.4300996", "-77.804161", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetPollenForecastAsync_ReturnsNull_WhenResponseInvalidJson()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid-json ", Encoding.UTF8, "application/json")
            };

            using var http = new HttpClient(new DelegatingHandlerStub(response));
            http.Timeout = TimeSpan.FromSeconds(30);

            var service = new GooglePollenService(http, "DUMMY_KEY", NullLogger<GooglePollenService>.Instance);

            // Act
            var result = await service.GetPollenForecastAsync("39.4300996", "-77.804161", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }
    }
}
