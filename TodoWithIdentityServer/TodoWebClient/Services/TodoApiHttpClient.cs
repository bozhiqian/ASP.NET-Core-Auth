using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace TodoWebClient.Services
{
    public class TodoApiHttpClient : ITodoApiHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();

        public TodoApiHttpClient(IHttpContextAccessor httpContextAccessor)
        {
            // In order to get access token via HttpContext.Authentication, so here we need to access HttpContext.
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<System.Net.Http.HttpClient> GetClient()
        {
            // In order to access API, the web client needs passing an Access Token to API. And the token should be passed as bearer token.

            string accessToken = await GetValidAccessToken();
            if(!string.IsNullOrEmpty(accessToken))
            {
                // Passing an access token to the web api on each request.
                _httpClient.SetBearerToken(accessToken); // The token should be passed as bearer token.
            }

            _httpClient.BaseAddress = new Uri("https://localhost:44396/"); // this is url of web api.
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return _httpClient;
        }

        private async Task<string> GetValidAccessToken()
        {
            var currentContext = _httpContextAccessor.HttpContext;
            var expiresAtToken = await currentContext.GetTokenAsync("expires_at");

            // and we check if our access token is going to expire in 60 seconds or less.
            var expiresAt = string.IsNullOrWhiteSpace(expiresAtToken) ? DateTime.MinValue : DateTime.Parse(expiresAtToken).AddSeconds(-60).ToUniversalTime();

            // If it's almost going to expire, we call into our RenewTokens method that will return a refreshed access token. 
            // If it's not about to expire, we can just get token from the authentication object on the context.
            string accessToken = await (expiresAt < DateTime.UtcNow ?
                RenewTokens() : currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken)); // the 'OpenIdConnectParameterNames.AccessToken'("access_token") is the name of the access token.
            return accessToken;
        }

        // Use the refresh token to get a new access token when the current access token has almost expired.
        private async Task<string> RenewTokens()
        {
            // get the current HttpContext to access the saved tokens
            var currentContext = _httpContextAccessor.HttpContext;

            // get the metadata
            var discoveryClient = new DiscoveryClient("https://localhost:44327/"); // Url of identityServer.
            var metaDataResponse = await discoveryClient.GetAsync();

            // then create the token client to get new tokens
            var tokenClient = new TokenClient(metaDataResponse.TokenEndpoint, "TodoWebClient", "ItsMySecret");

            // then we get the saved refresh token which is saved on the current context authentication object.
            var currentRefreshToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);

            // Refresh the tokens. If the refresh token is still valid, this call will result in new tokens being returned to us in the token result variable.
            var tokenResult = await tokenClient.RequestRefreshTokenAsync(currentRefreshToken); // Requests a PoP token using a refresh token.

            // if that RequestRefreshTokenAsync call was successful, we want to get the tokens from the token result 
            // and overwrite the current tokens we have in our authentication object.
            if (!tokenResult.IsError) 
            {
                // get current tokens
                var old_id_token = await currentContext.GetTokenAsync("id_token");

                var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);

                // get new tokens and expiration time
                var tokens = new List<AuthenticationToken>
                {
                    new AuthenticationToken
                    {
                        Name = OpenIdConnectParameterNames.IdToken,
                        Value = old_id_token
                    },
                    new AuthenticationToken
                    {
                        Name = OpenIdConnectParameterNames.AccessToken,
                        Value = tokenResult.AccessToken // new_access_token
                    },
                    new AuthenticationToken
                    {
                        Name = OpenIdConnectParameterNames.RefreshToken,
                        Value = tokenResult.RefreshToken // new_refresh_token
                    },
                    new AuthenticationToken
                    {
                        Name = "expires_at",
                        Value = expiresAt.ToString("o", CultureInfo.InvariantCulture)
                    }
                };

                // store tokens and sign in with renewed tokens
                var authenticateResult = await currentContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme); // "Cookies"
                authenticateResult.Properties.StoreTokens(tokens); // Stores a set of authentication tokens, after removing any old tokens.
                await currentContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticateResult.Principal, authenticateResult.Properties);

                // return the new access token 
                return tokenResult.AccessToken;
            }
            else
            {
                throw new Exception("Problem encountered while refreshing tokens.", tokenResult.Exception);
            }
        }
    }
}

