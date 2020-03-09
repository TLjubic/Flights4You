using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Flights4You.Web.Models;
using System.Net.Http;
using System.Net;
using Model;
using System.Net.Http.Headers;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Flights4You.Web.Controllers
{
    public class HomeController : Controller
    {
        public static int counter = 0;
        public static string tokenKey = "";
        private IMemoryCache _cache;

        public HomeController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        public IActionResult Index()
        {
            ViewData["Data"] = "data";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetApi(string departure, string destination, DateTime date, int passengers, string currency)
        {
            MyJsonObject cacheEntry;

            // Create unique cache key as a string combined with given parameters
            var keyString = departure + destination + date;
            if (passengers != 0)
            {
                keyString += passengers;
                ViewData["Passengers"] = passengers;
            }
            else
            {
                keyString += 1;
                ViewData["Passengers"] = 1;
            }
            if (currency != null)
            {
                keyString += currency;
            }

            // Look for cache key
            if (!_cache.TryGetValue(keyString, out cacheEntry))
            {
                var httpClient = HttpClientFactory.Create();

                // Formate date and time to ISO 8601 and use only date
                var newDate = date.ToString("s");
                var strDate = newDate.Substring(0, 10);

                // Create Api Get string from given parameters
                var urlString = "https://test.api.amadeus.com/v1/shopping/flight-offers?origin=" + departure + "&destination="
                    + destination + "&departureDate=" + strDate;

                if (passengers > 0)
                {
                    urlString = urlString + "&adults=" + passengers;                 
                }
                if (currency != null)
                {
                    urlString += "&currency=" + currency;
                }

                // Generate token access key on the beginning
                if (counter == 0)
                {
                    tokenKey = await GetTokenStringAsync();
                }
                
                // Get access on authorization and response message
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(urlString);
           
                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {  
                    cacheEntry = await GetDeserializeObjectAsync(cacheEntry, httpResponseMessage);
                }
                // If token access key is expired, generate new one
                else
                {
                    tokenKey = await GetTokenStringAsync();

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                    httpResponseMessage = await httpClient.GetAsync(urlString);

                    if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        cacheEntry = await GetDeserializeObjectAsync(cacheEntry, httpResponseMessage);
                    }
                }

                // Set cache options
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed
                    .SetSlidingExpiration(TimeSpan.FromSeconds(3000));

                // Save data in cache
                _cache.Set(keyString, cacheEntry, cacheEntryOptions);

            }
            else
            {
                ViewData["Data"] = "cacheData";
            }

            return View("Index", cacheEntry);
        }

        public async Task<string> GetTokenStringAsync()
        {
            var httpClient = HttpClientFactory.Create();
            Token token = new Token();

            // General data for getting token access key 
            string baseAddress = @"https://test.api.amadeus.com/v1/security/oauth2/token";
            string grant_type = "client_credentials";
            string client_id = "OOE6I2IaGIbTKCgUIjDGiyQO1gTlZupt";
            string client_secret = "qPEGNNddYHERWh8U";

            var form = new Dictionary<string, string>
                {
                    {"grant_type", grant_type},
                    {"client_id", client_id},
                    {"client_secret", client_secret},
                };

            // Post request for new token access key 
            HttpResponseMessage tokenResponse = await httpClient.PostAsync(baseAddress, new FormUrlEncodedContent(form));
            var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            token = JsonConvert.DeserializeObject<Token>(jsonContent);

            counter++;

            return token.Access_token;

        }

        public async Task<MyJsonObject> GetDeserializeObjectAsync(MyJsonObject cacheEntry, HttpResponseMessage httpResponseMessage)
        {
            var content = httpResponseMessage.Content;
            var data = await content.ReadAsStringAsync();

            cacheEntry = JsonConvert.DeserializeObject<MyJsonObject>(data);

            ViewData["Data"] = "apiData";

            return cacheEntry;
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
