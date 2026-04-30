# Xcalibur.Weather.Services

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/Xcalibur.Weather.Services.svg)](https://www.nuget.org/packages/Xcalibur.Weather.Services/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE-2.0.txt)

A comprehensive .NET library providing HTTP client services for weather-related APIs. Seamless integration with multiple weather data providers including Open-Meteo, Geocodio, and IpGeolocation.io for weather forecasting, geocoding, air quality monitoring, and astronomical data.

**Created by**: Joshua Arzt | **Company**: Xcalibur Systems, LLC.

## Latest Updates

- Package version: `1.0.2`
- Added `SunriseSunsetService` for sunrise and sunset data
- Added `OpenStreetMapService` for Nominatim geocoding

## 📋 Table of Contents

- [Features](#-features)
- [Installation](#-installation)
- [Services](#-services)
  - [OpenMeteoService](#openmeteoservice)
  - [GeocodioService](#geocodioservice)
  - [IpGeoService](#ipgeoservice)
  - [SunriseSunsetService](#sunrisesunsetservice)
  - [OpenStreetMapService](#openstreetmapservice)
- [Usage](#-usage)
  - [Basic Setup](#basic-setup)
  - [OpenMeteo Examples](#openmeteo-examples)
  - [Geocodio Examples](#geocodio-examples)
  - [IpGeo Examples](#ipgeo-examples)
  - [SunriseSunset Examples](#sunrisesunset-examples)
  - [OpenStreetMap Examples](#openstreetmap-examples)
- [API Endpoints](#-api-endpoints)
- [Dependencies](#-dependencies)
- [Testing](#-testing)
- [API Key Management](#-api-key-management)
- [Best Practices](#-best-practices)
- [Advanced Configuration](#-advanced-configuration)
- [Project Structure](#-project-structure)
- [License](#-license)
- [Related Projects](#-related-projects)

## ✨ Features

- **Multiple Weather Providers**: Integrated support for Open-Meteo, Geocodio, and IpGeolocation.io APIs
- **Comprehensive Weather Data**: Access current weather, forecasts, air quality, and astronomical data
- **Modern .NET 10**: Built with the latest .NET features and best practices
- **Async/Await**: Full asynchronous API support with cancellation tokens
- **Logging Support**: Built-in logging using Microsoft.Extensions.Logging
- **AOT-Ready**: Source-generated JSON contexts for Native AOT compilation support
- **Error Handling**: Robust error handling with detailed logging
- **Streaming Deserialization**: Efficient memory usage with streaming JSON deserialization
- **Type-Safe**: Strongly-typed responses using Xcalibur.Weather.Models
- **Flexible Provider Coverage**: Includes both API key and no-key providers for geocoding and astronomy data

## 📦 Installation

Install via NuGet Package Manager:

```bash
dotnet add package Xcalibur.Weather.Services
```

Or via Package Manager Console:

```powershell
Install-Package Xcalibur.Weather.Services
```

## 🌦️ Services

### OpenMeteoService

The `OpenMeteoService` provides access to Open-Meteo weather APIs, offering comprehensive weather data without requiring an API key.

**Key Features:**
- Current weather conditions
- Current air quality index (AQI)
- 48-hour hourly forecasts
- Multi-day daily forecasts
- Historical weather data (yesterday)

**Supported Data Points:**
- Temperature (current, apparent, min/max)
- Humidity and dew point
- Precipitation (rain, showers, snowfall)
- Wind (speed, direction, gusts)
- Atmospheric pressure
- Cloud cover and visibility
- Weather codes
- Air quality metrics (PM2.5, PM10, CO, NO2, SO2, O3, etc.)
- Pollen levels (alder, birch, grass, mugwort, olive, ragweed)
- UV index
- Sunrise/sunset and daylight duration

### GeocodioService

The `GeocodioService` provides geocoding capabilities to convert addresses into geographic coordinates.

**Key Features:**
- Forward geocoding (address to coordinates)
- Country-specific searches
- API key validation
- Detailed location results with accuracy information

### IpGeoService

The `IpGeoService` provides astronomical data for specific geographic locations.

**Key Features:**
- Sunrise and sunset times
- Moonrise and moonset times
- Moon phase information
- API key validation

### SunriseSunsetService

The `SunriseSunsetService` provides sunrise and sunset data from SunriseSunset.io without requiring an API key.

**Key Features:**
- Sunrise and sunset times
- Solar noon and day length data
- No API key required
- Lightweight astronomy lookups by coordinates

### OpenStreetMapService

The `OpenStreetMapService` provides geocoding through OpenStreetMap Nominatim.

**Key Features:**
- Forward geocoding (address to coordinates)
- Address details in results
- Country-filtered searches
- No API key required
- Built-in default `User-Agent` support for Nominatim requests

## 🚀 Usage

### Basic Setup

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Xcalibur.Weather.Services.WeatherProvider.OpenMeteo;
using Xcalibur.Weather.Services.WeatherProvider.Geocodio;
using Xcalibur.Weather.Services.WeatherProvider.IpGeo;
using Xcalibur.Weather.Services.WeatherProvider.SunriseSunset;
using Xcalibur.Weather.Services.WeatherProvider.OpenStreetMap;

var httpClient = new HttpClient();
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<OpenMeteoService>();

var openMeteoService = new OpenMeteoService(httpClient, logger);
var geocodioService = new GeocodioService(httpClient, "YOUR_GEOCODIO_API_KEY", loggerFactory.CreateLogger<GeocodioService>());
var ipGeoService = new IpGeoService(httpClient, "YOUR_IPGEO_API_KEY", loggerFactory.CreateLogger<IpGeoService>());
var sunriseSunsetService = new SunriseSunsetService(httpClient, loggerFactory.CreateLogger<SunriseSunsetService>());
var openStreetMapService = new OpenStreetMapService(httpClient, NullLogger.Instance);
```

### OpenMeteo Examples

#### Get Current Weather

```csharp
var currentWeather = await openMeteoService.GetCurrentWeatherAsync("40.7128", "-74.0060");

if (currentWeather?.Current != null)
{
    Console.WriteLine($"Temperature: {currentWeather.Current.Temperature2m}°C");
    Console.WriteLine($"Humidity: {currentWeather.Current.RelativeHumidity2m}%");
    Console.WriteLine($"Wind Speed: {currentWeather.Current.WindSpeed10m} km/h");
}
```

#### Get Current Air Quality

```csharp
var airQuality = await openMeteoService.GetCurrentAirQualityAsync("40.7128", "-74.0060");

if (airQuality?.Current != null)
{
    Console.WriteLine($"US AQI: {airQuality.Current.UsAqi}");
    Console.WriteLine($"PM2.5: {airQuality.Current.Pm25}");
    Console.WriteLine($"PM10: {airQuality.Current.Pm10}");
}
```

#### Get Hourly Forecast

```csharp
var hourlyForecast = await openMeteoService.GetHourlyForecastAsync("40.7128", "-74.0060");

if (hourlyForecast?.Hourly != null)
{
    for (int i = 0; i < hourlyForecast.Hourly.Time.Length; i++)
    {
        Console.WriteLine($"{hourlyForecast.Hourly.Time[i]}: {hourlyForecast.Hourly.Temperature2m[i]}°C");
    }
}
```

#### Get Daily Forecast

```csharp
// Get 7-day forecast
var dailyForecast = await openMeteoService.GetDailyForecastAsync("40.7128", "-74.0060", 7);

if (dailyForecast?.Daily != null)
{
    for (int i = 0; i < dailyForecast.Daily.Time.Length; i++)
    {
        Console.WriteLine($"{dailyForecast.Daily.Time[i]}:");
        Console.WriteLine($"  High: {dailyForecast.Daily.Temperature2mMax[i]}°C");
        Console.WriteLine($"  Low: {dailyForecast.Daily.Temperature2mMin[i]}°C");
        Console.WriteLine($"  Precipitation: {dailyForecast.Daily.PrecipitationSum[i]}mm");
    }
}
```

#### Get Yesterday's Weather

```csharp
var yesterday = DateTime.UtcNow.AddDays(-1);
var historicalWeather = await openMeteoService.GetYesterdayForecastAsync(
    "40.7128", 
    "-74.0060", 
    yesterday.ToString("yyyy-MM-dd"));

if (historicalWeather?.Hourly != null)
{
    Console.WriteLine($"Yesterday's temperatures:");
    for (int i = 0; i < historicalWeather.Hourly.Time.Length; i++)
    {
        Console.WriteLine($"{historicalWeather.Hourly.Time[i]}: {historicalWeather.Hourly.Temperature2m[i]}°C");
    }
}
```

### Geocodio Examples

#### Setup with API Key

```csharp
var geocodioService = new GeocodioService(
    httpClient, 
    "YOUR_GEOCODIO_API_KEY", 
    loggerFactory.CreateLogger<GeocodioService>());
```

#### Test API Key

```csharp
var isValid = await geocodioService.TestApiKey();
Console.WriteLine($"API Key is {(isValid ? "valid" : "invalid")}");
```

#### Geocode an Address

```csharp
var locations = await geocodioService.GetLocationsAsync(
    "1600 Pennsylvania Avenue NW, Washington, DC", 
    "US");

if (locations?.Results != null)
{
    foreach (var result in locations.Results)
    {
        Console.WriteLine($"Location: {result.FormattedAddress}");
        Console.WriteLine($"Coordinates: {result.Location.Lat}, {result.Location.Lng}");
        Console.WriteLine($"Accuracy: {result.Accuracy}");
    }
}
```

### IpGeo Examples

#### Setup with API Key

```csharp
var ipGeoService = new IpGeoService(
    httpClient, 
    "YOUR_IPGEO_API_KEY", 
    loggerFactory.CreateLogger<IpGeoService>());
```

#### Test API Key

```csharp
var isValid = await ipGeoService.TestApiKey();
Console.WriteLine($"API Key is {(isValid ? "valid" : "invalid")}");
```

#### Get Astronomical Data

```csharp
var sunMoonData = await ipGeoService.GetSunMoonDataAsync("40.7128", "-74.0060");

if (sunMoonData != null)
{
    Console.WriteLine($"Sunrise: {sunMoonData.Sunrise}");
    Console.WriteLine($"Sunset: {sunMoonData.Sunset}");
    Console.WriteLine($"Moonrise: {sunMoonData.Moonrise}");
    Console.WriteLine($"Moonset: {sunMoonData.Moonset}");
    Console.WriteLine($"Moon Phase: {sunMoonData.MoonPhase}");
}
```

### SunriseSunset Examples

#### Setup

```csharp
var sunriseSunsetService = new SunriseSunsetService(
    httpClient,
    loggerFactory.CreateLogger<SunriseSunsetService>());
```

#### Get Sunrise and Sunset Data

```csharp
var astronomy = await sunriseSunsetService.GetSunriseSunsetAsync("40.7128", "-74.0060");

if (astronomy?.Results != null)
{
    Console.WriteLine($"Sunrise: {astronomy.Results.Sunrise}");
    Console.WriteLine($"Sunset: {astronomy.Results.Sunset}");
    Console.WriteLine($"Day Length: {astronomy.Results.DayLength}");
    Console.WriteLine($"Solar Noon: {astronomy.Results.SolarNoon}");
}
```

### OpenStreetMap Examples

#### Setup

```csharp
var openStreetMapService = new OpenStreetMapService(
    httpClient,
    loggerFactory.CreateLogger<OpenStreetMapService>());
```

#### Search for Locations

```csharp
var locations = await openStreetMapService.GetLocationsAsync(
    "1600 Pennsylvania Avenue NW, Washington, DC",
    "us");

if (locations != null)
{
    foreach (var location in locations)
    {
        Console.WriteLine($"Display Name: {location.DisplayName}");
        Console.WriteLine($"Coordinates: {location.Lat}, {location.Lon}");
    }
}
```

## 🌐 API Endpoints

### Open-Meteo
- **Current Weather**: `https://api.open-meteo.com/v1/forecast`
- **Air Quality**: `https://air-quality-api.open-meteo.com/v1/air-quality`
- **Historical**: `https://archive-api.open-meteo.com/v1/archive`
- **API Key**: Not required (free and open)
- **Documentation**: [Open-Meteo API Docs](https://open-meteo.com/en/docs)

### Geocodio
- **Base URL**: `https://api.geocod.io/v1.9/geocode`
- **API Key**: Required
- **Sign Up**: [Geocodio](https://www.geocod.io/)
- **Documentation**: [Geocodio API Docs](https://www.geocod.io/docs/)

### IpGeolocation.io
- **Base URL**: `https://api.ipgeolocation.io/v2/astronomy`
- **API Key**: Required
- **Sign Up**: [IpGeolocation.io](https://ipgeolocation.io/)
- **Documentation**: [IpGeo API Docs](https://ipgeolocation.io/documentation/astronomy-api.html)

### SunriseSunset.io
- **Base URL**: `https://api.sunrisesunset.io/json`
- **API Key**: Not required
- **Documentation**: [SunriseSunset.io Docs](https://sunrisesunset.io/api/)

### OpenStreetMap Nominatim
- **Base URL**: `https://nominatim.openstreetmap.org/search`
- **API Key**: Not required
- **Requirement**: A descriptive `User-Agent` header is required
- **Documentation**: [Nominatim Search API](https://nominatim.org/release-docs/latest/api/Search/)

## 📚 Dependencies

- **.NET 10.0**: Target framework
- **Microsoft.Extensions.Hosting** (v10.0.7): For hosting, logging, and dependency injection abstractions
- **Xcalibur.Weather.Models**: Shared models and DTOs for weather data

## 🧪 Testing

The project includes comprehensive unit tests in the `Xcalibur.Weather.Services.Tests` project.

### Running Tests

```bash
dotnet test
```

### Current Test Coverage

- `OpenMeteoServiceTests`: Tests for Open-Meteo API operations
- `GeocodioServiceTests`: Tests for geocoding functionality and API key validation
- `IpGeoServiceTests`: Tests for astronomical data retrieval

## 🏗️ Project Structure

```
Xcalibur.Weather.Services/
├── WeatherProvider/
│   ├── OpenMeteo/
│   │   └── OpenMeteoService.cs
│   ├── Geocodio/
│   │   └── GeocodioService.cs
│   ├── IpGeo/
│   │   └── IpGeoService.cs
│   ├── SunriseSunset/
│   │   └── SunriseSunsetService.cs
│   └── OpenStreetMap/
│       └── OpenStreetMapService.cs
├── Xcalibur.Weather.Services.csproj
│
Xcalibur.Weather.Services.Tests/
├── WeatherProvider/
│   ├── OpenMeteoServiceTests.cs
│   ├── GeocodioServiceTests.cs
│   └── IpGeoServiceTests.cs
└── Xcalibur.Weather.Services.Tests.csproj
```

## 🔧 Advanced Configuration

### Custom HttpClient Configuration

```csharp
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)
};

httpClient.DefaultRequestHeaders.Add("User-Agent", "YourApp/1.0");

var service = new OpenMeteoService(httpClient, logger);
```

### Cancellation Token Support

All async methods support cancellation tokens for graceful shutdown:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

try
{
    var weather = await openMeteoService.GetCurrentWeatherAsync(
        "40.7128", 
        "-74.0060", 
        cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request was cancelled");
}
```

## 📝 Best Practices

1. **Reuse HttpClient**: Create a single `HttpClient` instance and reuse it across service instances
2. **Use Dependency Injection**: Register services in your DI container for better testability
3. **Handle Nulls**: All service methods return nullable types; always check for null responses
4. **Monitor Logs**: Enable debug logging to troubleshoot API issues
5. **Respect Provider Policies**: Be mindful of rate limits and usage policies for Geocodio, IpGeo, SunriseSunset.io, and OpenStreetMap Nominatim
6. **Secure API Keys**: Store API keys in secure configuration (Azure Key Vault, user secrets, etc.)
7. **Set a User-Agent When Needed**: OpenStreetMap Nominatim requires a meaningful `User-Agent`; the service sets a default if one is not already present

## 🔐 API Key Management

### User Secrets (Development)

```bash
dotnet user-secrets init
dotnet user-secrets set "Geocodio:ApiKey" "YOUR_API_KEY"
dotnet user-secrets set "IpGeo:ApiKey" "YOUR_API_KEY"
```

### appsettings.json

```json
{
  "Geocodio": {
    "ApiKey": "YOUR_GEOCODIO_API_KEY"
  },
  "IpGeo": {
    "ApiKey": "YOUR_IPGEO_API_KEY"
  }
}
```

### Environment Variables

```bash
set GEOCODIO_API_KEY=your_key_here
set IPGEO_API_KEY=your_key_here
```

## 📄 License

This project is licensed under the Apache License 2.0. See the [LICENSE-2.0.txt](LICENSE-2.0.txt) file for details.

Copyright © 2006 - 2026, Xcalibur Systems, LLC - All Rights Reserved

## 🔗 Related Projects

- **[Xcalibur.Weather.Models](https://www.nuget.org/packages/Xcalibur.Weather.Models/)** - Core weather data models and DTOs ([GitHub](https://github.com/Xcalibur37/Xcalibur.Weather.Models))
- **[Xcalibur.Weather.Helpers](https://www.nuget.org/packages/Xcalibur.Weather.Helpers/)** - Utility functions and conversion helpers ([GitHub](https://github.com/Xcalibur37/Xcalibur.Weather.Helpers))

---

*Part of the Xcalibur Weather ecosystem for comprehensive weather data integration.*

## Author

**Joshua Arzt**  
Xcalibur Systems, LLC

---

**Note**: This library requires API keys for Geocodio and IpGeolocation.io services. Open-Meteo, SunriseSunset.io, and OpenStreetMap Nominatim do not require API keys.
