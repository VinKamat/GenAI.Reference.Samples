using Microsoft.SemanticKernel;

namespace SKAgentApp.Core
{
    /// <summary>
    /// Provides weather data from weatherapi.com using Semantic Kernel's KernelFunction invokation
    /// </summary>
    internal class WeatherService
    {
        private string _weatherApiKey;

        public WeatherService(string weatherApiKey)
        {
            this._weatherApiKey = weatherApiKey;
        }

        // Add a method a kernel function to get detailed weather report data from weatherapi.com
        [KernelFunction]
        public async Task<string> GetWeather(string location)
        {
            // TODO: api url should be in App Settings and retrieved from there via Configuration
            string apiUrl = $"https://api.weatherapi.com/v1/current.json?key={_weatherApiKey}&q={location}&aqi=no";
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                string weatherData = await response.Content.ReadAsStringAsync();
                return weatherData;
            }
        }
    }
}
