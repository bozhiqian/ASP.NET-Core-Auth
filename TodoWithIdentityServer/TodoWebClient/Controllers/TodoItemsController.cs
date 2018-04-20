using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using TodoViewModel;
using TodoWebClient.Services;
using TodoWebClient.Token;
using TodoWebClient.ViewModels;

namespace TodoWebClient.Controllers
{
    [Authorize]
    public class TodoItemsController : Controller
    {
        private readonly ITodoApiHttpClient _iTodoApiHttpClient;

        public TodoItemsController(ITodoApiHttpClient todoApiHttpClient)
        {
            _iTodoApiHttpClient = todoApiHttpClient;
        }

        public async Task<IActionResult> Index()
        {
            await WriteOutIdentityInformation();

            // call the API
            var httpClient = await _iTodoApiHttpClient.GetClient();

            var response = await httpClient.GetAsync("api/todoitems").ConfigureAwait(false);
            return await HandleApiResponseAsync(response, async () =>
            {
                if (response.IsSuccessStatusCode)
                {
                    var todoitemsAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var todoItemsIndexViewModel = new TodoItemsIndexViewModel(
                        JsonConvert.DeserializeObject<IList<TodoItemViewModel>>(todoitemsAsString).ToList());

                    return View(todoItemsIndexViewModel);

                }
                else // if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Authorization");
                }
            });
        }

        public async Task<IActionResult> EditTodoItem(long id)
        {
            // call the API
            var httpClient = await _iTodoApiHttpClient.GetClient();

            var response = await httpClient.GetAsync($"api/todoitmes/{id}").ConfigureAwait(false);
            return await HandleApiResponseAsync(response, async () =>
            {
                var todoitemsAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var deserializedTodoItemViewModel = JsonConvert.DeserializeObject<TodoItemViewModel>(todoitemsAsString);

                var editTodoItemViewModel = new EditTodoItemViewModel
                {
                    Id = deserializedTodoItemViewModel.Id,
                    Name = deserializedTodoItemViewModel.Name,
                    IsComplete = deserializedTodoItemViewModel.IsComplete
                };

                return View(editTodoItemViewModel);
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTodoItem(EditTodoItemViewModel editTodoItemViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // create an TodoItemForUpdateViewModel instance
            var todoItemForUpdateViewModel = new TodoItemForUpdateViewModel { Name = editTodoItemViewModel.Name, IsComplete = editTodoItemViewModel.IsComplete };

            // serialize it
            var serializedTodoItemForUpdateViewModel = JsonConvert.SerializeObject(todoItemForUpdateViewModel);

            // call the API
            var httpClient = await _iTodoApiHttpClient.GetClient();

            var response = await httpClient.PutAsync(
                                               $"api/todoitems/{editTodoItemViewModel.Id}",
                                               new StringContent(serializedTodoItemForUpdateViewModel, Encoding.Unicode, "application/json"))
                                           .ConfigureAwait(false);
            return HandleApiResponse(response, () => RedirectToAction("Index"));
        }

        public async Task<IActionResult> DeleteTodoItem(long id)
        {
            // call the API
            var httpClient = await _iTodoApiHttpClient.GetClient();

            var response = await httpClient.DeleteAsync($"api/todoitems/{id}").ConfigureAwait(false);
            return HandleApiResponse(response, () => RedirectToAction("Index"));
        }


        [Authorize(Roles = "PayingUser")]
        public IActionResult AddTodoItem()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PayingUser")]
        public async Task<IActionResult> AddTodoItem(AddTodoItemViewModel addTodoItemViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // create an ImageForCreation instance
            var todoItemForCreationViewModel = new TodoItemForCreationViewModel() { Name = addTodoItemViewModel.Name };

            // serialize it
            var serializedTodoItemForCreationViewModel = JsonConvert.SerializeObject(todoItemForCreationViewModel);

            // call the API
            var httpClient = await _iTodoApiHttpClient.GetClient();

            var response = await httpClient.PostAsync(
                                               $"api/todoitems",
                                               new StringContent(serializedTodoItemForCreationViewModel, Encoding.Unicode, "application/json"))
                                           .ConfigureAwait(false);
            return HandleApiResponse(response, () => RedirectToAction("Index"));
        }

        public async Task Logout()
        {
            // get the metadata
            var discoveryClient = new DiscoveryClient("https://localhost:44327/"); // Url of IdentityServer.
            var metaDataResponse = await discoveryClient.GetAsync();


            #region // Token Revocation - Clients can programmatically revoke tokens in IdentityServer via the token revocation endpoint

            // get revocation client - Client for an OAuth 2.0 token revocation endpoint
            var revocationClient = new TokenRevocationClient(metaDataResponse.RevocationEndpoint, "TodoWebClient", "ItsMySecret");

            await revocationClient.RevokeAccessTokenAsync(HttpContext); 
            await revocationClient.RevokeRefreshTokenAsync(HttpContext); 
            #endregion

            #region // sign-out of authentication schemes

            // Log out of web client.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // "Cookies" // the scheme name must match the one at AddCookie() specified in 'Startup.cs'.

            // Log out of the identity provider too.
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme); // "OpenIdConnect"// the scheme name must match the one at AddOpenIdConnect() specified in 'Startup.cs'.

            #endregion
        }

        #region // Showing an Access Denied Page

        private async Task<IActionResult> HandleApiResponseAsync(HttpResponseMessage response, Func<Task<IActionResult>> onSuccess)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    {
                        return await onSuccess();
                    }
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    return RedirectToAction("AccessDenied", "Authorization");
                default:
                    throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
            }
        }

        private IActionResult HandleApiResponse(HttpResponseMessage response, Func<IActionResult> onSuccess)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.NoContent:
                case HttpStatusCode.Created:
                    {
                        return onSuccess();
                    }
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    return RedirectToAction("AccessDenied", "Authorization");
                default:
                    throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
            }
        }
        #endregion

        public async Task WriteOutIdentityInformation()
        {
            var identityToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);
            Debug.WriteLine($"IdentityToken: {identityToken}");
            foreach (var claim in User.Claims)
            {
                Debug.WriteLine($"Claim type: {claim.Type}, claim value: {claim.Value}");
            }
        }

    }
}