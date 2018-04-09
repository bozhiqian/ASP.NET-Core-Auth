using System;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TodoWeb.AzureAdB2C;

namespace TodoWeb
{
    public class Startup
    {
        private readonly AzureAdB2COptions _azureAdB2COptions;
        public Startup(IConfiguration configuration, AzureAdB2COptions azureAdB2COptions)
        {
            Configuration = configuration;

            _azureAdB2COptions = azureAdB2COptions;

            Configuration.Bind("Authentication:AzureAdB2C", _azureAdB2COptions);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.Configure<AzureAdB2COptions>(options => Configuration.Bind("Authentication:AzureAdB2C", options));

            services.AddAuthentication(sharedOptions =>
                {
                    sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    sharedOptions.DefaultChallengeScheme = _azureAdB2COptions.AuthenticationScheme; //OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddOpenIdConnect(_azureAdB2COptions.AuthenticationScheme, //"AzureAdB2C",
                    options =>
                    {
                        SetOptionsForOpenIdConnectPolicy(_azureAdB2COptions.DefaultPolicy, options, _azureAdB2COptions);
                    })
                .AddCookie();

            services.AddMvc();

            // Adds a default in-memory implementation of IDistributedCache.
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(1);
                options.CookieHttpOnly = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseSession();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public Task OnRedirectToIdentityProvider(RedirectContext context, string defaultPolicy, string apiUrl, string apiScopes)
        {
            if (context.Properties.Items.TryGetValue(AzureAdB2COptions.PolicyAuthenticationProperty, out var policy) &&
                !policy.Equals(defaultPolicy))
            {
                context.ProtocolMessage.Scope = OpenIdConnectScope.OpenIdProfile;
                context.ProtocolMessage.ResponseType = OpenIdConnectResponseType.IdToken;
                context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress.ToLower().Replace(defaultPolicy.ToLower(), policy.ToLower());
                context.Properties.Items.Remove(AzureAdB2COptions.PolicyAuthenticationProperty);
            }
            else 
            {
                context.ProtocolMessage.Scope += $" offline_access {apiScopes}";
                context.ProtocolMessage.ResponseType = OpenIdConnectResponseType.CodeIdTokenToken;
            }

            return Task.FromResult(0);
        }

        public Task OnRemoteFailure(RemoteFailureContext context)
        {
            context.HandleResponse();
            // Handle the error code that Azure AD B2C throws when trying to reset a password from the login page 
            // because password reset is not supported by a "sign-up or sign-in policy"
            if (context.Failure is OpenIdConnectProtocolException && context.Failure.Message.Contains("AADB2C90118"))
            {
                context.Response.Redirect("/Session/ResetPassword");
            }
            else if (context.Failure is OpenIdConnectProtocolException &&
                     context.Failure.Message.Contains("access_denied"))
            {
                context.Response.Redirect("/");
            }
            else
            {
                // https://github.com/Azure-Samples/active-directory-b2c-dotnetcore-webapp/issues/29
                var message = Regex.Replace(context.Failure.Message, @"[^\u001F-\u007F]+", string.Empty);
                context.Response.Redirect("/Home/Error?message=" + message);
                // context.Response.Redirect("/Home/Error?message=" + context.Failure.Message);

                /* if you have this exception: 
                 * Message contains error: 'invalid_request', error_description: 'AADB2C90205: This application does not have sufficient permissions against this web resource to perform the operation.
                 * Correlation ID: 073af821-4d5c-4db1-9d51-5f57d2c148e2Timestamp: 2018-04-09 09:37:13Z', error_uri: 'error_uri is null'.
                 *
                 * Please check this https://github.com/Azure-Samples/active-directory-b2c-javascript-msal-singlepageapp/issues/4
                */
            }

            return Task.FromResult(0);
        }

        public async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedContext context, string clientId, string authority, string clientSecret, string redirectUri, string apiScopes)
        {
            // Use MSAL to swap the code for an access token
            // Extract the code from the response notification
            var code = context.ProtocolMessage.Code;

            var signedInUserId = context.Principal.FindFirst(ClaimTypes.NameIdentifier).Value;
            var userTokenCache = new MsalSessionCache(signedInUserId, context.HttpContext).GetMsalCacheInstance();
            var cca = new ConfidentialClientApplication(clientId, authority, redirectUri, new ClientCredential(clientSecret), userTokenCache, null);
            try
            {
                var result =
                    await cca.AcquireTokenByAuthorizationCodeAsync(code, apiScopes.Split(' '));


                context.HandleCodeRedemption(result.AccessToken, result.IdToken);
            }
            catch (Exception ex)
            {
                //TODO: Handle
                throw;
            }
        }

        public void SetOptionsForOpenIdConnectPolicy(string policy, OpenIdConnectOptions options, AzureAdB2COptions azureAdB2COptions)
        {
            options.ClientId = azureAdB2COptions.ApplicationId; // Azure AD B2C application ID."### ADD APPLICATION ID HERE ###";
            options.Authority = azureAdB2COptions.Authority;
            options.UseTokenLifetime = true;
            options.TokenValidationParameters = new TokenValidationParameters { NameClaimType = "name" };

            options.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = context => OnRedirectToIdentityProvider(context, azureAdB2COptions.DefaultPolicy, azureAdB2COptions.ApiUrl, azureAdB2COptions.ApiScopes),
                OnRemoteFailure = OnRemoteFailure,
                //OnAuthorizationCodeReceived = OnAuthorizationCodeReceived
                OnAuthorizationCodeReceived = async context => await OnAuthorizationCodeReceived(context,
                    azureAdB2COptions.ApplicationId,
                    azureAdB2COptions.Authority,
                    azureAdB2COptions.ClientSecret,
                    azureAdB2COptions.RedirectUri,
                    azureAdB2COptions.ApiScopes)
            };

            // https://login.microsoftonline.com/bqadb2c.onmicrosoft.com/oauth2/v2.0/authorize?p=B2C_1_sign_up_in&client_id=50b51705-27f1-4cf3-b9a1-1ee8be89bc8c&nonce=defaultNonce&redirect_uri=msal50b51705-27f1-4cf3-b9a1-1ee8be89bc8c%3A%2F%2Fauth&scope=openid&response_type=id_token&prompt=login
            options.MetadataAddress = $"https://login.microsoftonline.com/{azureAdB2COptions.Tenant}/v2.0/.well-known/openid-configuration?p={policy}";
        }
    }
}

// https://github.com/Azure-Samples/active-directory-b2c-dotnetcore-webapp
// https://azure.microsoft.com/en-us/resources/samples/active-directory-b2c-dotnetcore-webapi/
// https://github.com/Azure-Samples/active-directory-b2c-dotnetcore-webapi
// https://github.com/Azure-Samples/active-directory-b2c-xamarin-native

// https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-app-registration
// https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-devquickstarts-web-dotnet-susi


// https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-get-started
// https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-reference-oidc
// https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-reference-oauth-code
// https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-reference-policies
// https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-access-tokens
// https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-v2-scopes



// https://blogs.msdn.microsoft.com/gianlucb/2017/10/04/access-an-azure-ad-secured-api-with-asp-net-core-2-0/
