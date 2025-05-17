using Microsoft.SemanticKernel;

namespace SKAgentApp.Core
{
    /// <summary>
    /// Provides weather data from weatherapi.com using Semantic Kernel's KernelFunction invokation
    /// </summary>
    internal class WeatherService
    {
        private readonly string _weatherApiKey;
        private readonly string _weatherApiUrl;

        public WeatherService(string weatherApiKey, string weatherApiUrl)
        {
            _weatherApiKey = weatherApiKey;
            _weatherApiUrl = weatherApiUrl;
        }

        // Add a method a kernel function to get detailed weather report data from weatherapi.com
        [KernelFunction]
        public async Task<string> GetWeather(string location)
        {
            string apiUrl = $"{_weatherApiUrl}?key={_weatherApiKey}&q={location}&aqi=no";
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                string weatherData = await response.Content.ReadAsStringAsync();
                return weatherData;
            }
        }
    }
}
