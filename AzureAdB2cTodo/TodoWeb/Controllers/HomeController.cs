using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TodoWeb.AzureAdB2C;
using TodoWeb.Models;

namespace TodoWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly AzureAdB2COptions _azureAdB2COptions;

        public HomeController(IOptions<AzureAdB2COptions> azureAdB2COptions)
        {
            _azureAdB2COptions = azureAdB2COptions.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult About()
        {
            ViewData["Message"] = string.Format("Claims available for the user {0}", User.FindFirst("name")?.Value);
            return View();
        }

        [Authorize]
        public async Task<IActionResult> TodoApi()
        {
            var responseString = "";
            try
            {
                var result = await AcquireTokenSilentAsync(_azureAdB2COptions);

                var apiUrl = _azureAdB2COptions.ApiUrl;

                var response = await RequestAsync(HttpMethod.Get, apiUrl, result.AccessToken);

                // Handle the response
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        string json = await response.Content.ReadAsStringAsync();
                        responseString = JToken.Parse(json).ToString(Formatting.Indented);
                        break;
                    case HttpStatusCode.Unauthorized:
                        responseString = $"Please sign in again. {response.ReasonPhrase}";
                        break;
                    default:
                        responseString = $"Error calling API. StatusCode=${response.StatusCode}";
                        break;
                }
            }
            catch (MsalUiRequiredException ex)
            {
                responseString = $"Session has expired. Please sign in again. {ex.Message}";
            }
            catch (Exception ex)
            {
                responseString = $"Error calling API: {ex.Message}";
            }

            ViewData["Payload"] = $"{responseString}";
            return View("Api");
        }

        public IActionResult Error(string message)
        {
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = message
            };

            return View(errorViewModel);
        }

        private async Task<AuthenticationResult> AcquireTokenSilentAsync(AzureAdB2COptions azureAdB2COptions)
        {
            // Retrieve the token with the specified scopes
            var scope = azureAdB2COptions.ApiScopes.Split(' ');
            var signedInUserId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var userTokenCache = new MsalSessionCache(signedInUserId, HttpContext).GetMsalCacheInstance();
            var cca = new ConfidentialClientApplication(
                azureAdB2COptions.ApplicationId,
                azureAdB2COptions.Authority,
                azureAdB2COptions.RedirectUri,
                new ClientCredential(azureAdB2COptions.ClientSecret), userTokenCache, null);

            var result = await cca.AcquireTokenSilentAsync(scope, cca.Users.FirstOrDefault(), azureAdB2COptions.Authority, false);
            return result;
        }

        private async Task<HttpResponseMessage> RequestAsync(HttpMethod method, string apiUrl, string accessToken)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(method, apiUrl);

            // Add token to the Authorization header and make the request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.SendAsync(request);

            return response;
        }
    }
}