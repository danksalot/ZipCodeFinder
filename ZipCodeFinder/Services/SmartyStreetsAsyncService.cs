using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ZipCodeFinder.Services
{
    public class SmartyStreetsAsyncService : IZipCodeService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration Configuration;

        public SmartyStreetsAsyncService(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            Configuration = configuration;
        }

        public async Task<List<string>> LookupZipCodes(string city, string state)
        {
            List<string> zipCodes = new List<string>();

            string apiKey = Configuration.GetValue<string>("SmartyStreetsApiKey");
            string url = $"https://us-zipcode.api.smartystreets.com/lookup?auth-id={apiKey}&city={city}&state={state}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/vnd.github.v3+json");
            request.Headers.Add("User-Agent", "HttpClientFactory-Sample");

            var client = _clientFactory.CreateClient();

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                List<SmartyStreetsZipCodeResult> results = await JsonSerializer.DeserializeAsync<List<SmartyStreetsZipCodeResult>>(responseStream);

                if (results.Count == 1 && results[0].zipcodes != null)
                {
                    zipCodes = results[0].zipcodes.Select(z => z.zipcode).ToList();
                }
            }

            return zipCodes;
        }
    }

    public class SmartyStreetsZipCodeResult
    {
        public List<SmartyStreetsZipCode> zipcodes { get; set; }
    }

    public class SmartyStreetsZipCode
    {
        public string zipcode { get; set; }
    }
}
