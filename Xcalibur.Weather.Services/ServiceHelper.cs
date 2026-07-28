using System;
using System.Collections.Generic;
using System.Text;

namespace Xcalibur.Weather.Services
{
    /// <summary>
    /// Provides helper methods for creating HTTP requests for weather alert APIs.
    /// </summary>
    public static class ServiceHelper
    {
        /// <summary>
        /// Creates an HTTP request with required headers for weather alert APIs.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        public static HttpRequestMessage CreateRequest(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", "Xcalibur.Weather/1.0 (weather-app; info@xcalibursystems.com)");
            return request;
        }
    }
}
