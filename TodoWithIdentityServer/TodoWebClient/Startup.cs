using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TodoWebClient.Services;

namespace TodoWebClient
{
    public class Startup
    {
        public readonly string Authority; // Identity Provider: IdentityServer4 Url.
        public readonly string ClientId; // ClientId in IdentityServer4.
        public readonly string ClientSecret;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            Authority = Configuration["Authority:IdentityServer4:Url"];
            ClientId = Configuration["Authority:IdentityServer4:ClientId"];
            ClientSecret = Configuration["Authority:IdentityServer4:ClientSecret"];
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddAuthorization(options =>
            {
                // Attribute-based Access Control(ABAC)
                options.AddPolicy("CanOrderFrame", policybuilder =>
                {
                    policybuilder.RequireAuthenticatedUser();
                    policybuilder.RequireClaim("subscriptionlevel", "PayingUser");
                    policybuilder.RequireClaim("country", "nl");
                });
            });

            // register an IHttpContextAccessor so we can access the current
            // HttpContext in services by injecting it
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // register an IImageGalleryHttpClient
            services.AddScoped<ITodoApiHttpClient, TodoApiHttpClient>();

            /*
             To add OpenID Connect authentication to a ASP.NET Core site
            */
            services.AddAuthentication(options =>
                    {
                        /*
                         Here we are telling our application to use cookie authentication, for signing in users, and to use it as the default method of authentication. 
                         Whilst we may be using IdentityServer to authenticate users, every client application still needs to issue its own cookie (to its own domain)
                        */
                        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // "Cookies";
                        // options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme; // "Cookies";

                        // OpenID Connect (OIDC) Authentication
                        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme; // "OpenIdConnect";
                    })

                    // Note: When sign out, need to HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                    {
                        options.AccessDeniedPath = "/Authorization/AccessDenied"; // Creating an Access Denied Page
                    })

                    /*
                     Here we are telling our app to use our OpenID Connect Provider (IdentityServer), the client id we wish to sign in with and the authentication type 
                     to login with upon successful authentication (our previously defined cookie middleware).

                     By default, the OpenID Connect middleware options will use /signin-oidc as its redirect uri, request the scopes openid and profile. 
                    */
                    // Note: When sign out, also need to HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                    {
                        // This is the scheme responsible for persisting the user's identity after sucessful authentication.
                        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme; //"Cookies";

                        // URI of the Identity Provider. It is the authority responsible or the identity provider part of the OIDC flows.
                        // The OIDC middleware will use this value to read the metadata on the discovery endpoint. So it knows where to find the different endpoints and othere information.
                        options.Authority = Authority; //"https://localhost:44327"; // Identity Provider: IdentityServer

                        options.RequireHttpsMetadata = true;
                        options.ClientId = ClientId; // ClientId is set in the 'Config.cs' of 'IdentityServer'

                        // These scopes are AllowedScopes defined in the given Client at 'Config.cs'. 
                        // We need to add these scopes so they are in the access token which we have to include in request to the userinfo endpoint. 
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.Scope.Add("address");
                        options.Scope.Add("roles");
                        options.Scope.Add("courses");
                        options.Scope.Add("TodoApi"); // add this resource scope to the requested list of scopes. 
                        options.Scope.Add("subscriptionlevel");
                        options.Scope.Add("country");
                        options.Scope.Add("offline_access"); // adding this because we set "AllowOfflineAccess = true," in "config.cs" from IdentityServer.

                        // We want to ensure we ask for an access token with this scope included.
                        options.ResponseType = OpenIdConnectResponseType.CodeIdToken; // "code id_token";
                                                                                      //options.CallbackPath = new PathString("...");

                        // options.CallbackPath = new PathString("..."); // if 'RedirectUris' in 'Config.cs' of IDP is not default 'signin-oidc', then we can give it new path name. 

                        // to set whether the handler should go to user info endpoint to retrieve additional claims or not 
                        // after creating an identity from id_token received from token endpoint. The default is 'false'.
                        options.GetClaimsFromUserInfoEndpoint = true;

                        options.SaveTokens = true; // This allows the middleware to save the tokens that it receives from the identity provider. 
                        options.ClientSecret = ClientSecret; //"ItsMySecret"; // It is set at 'Config.cs' of the 'IdentityServer'.

                        // set redirect url after signing out from Indentity Provider, but there's a default sign out call back OIDC. So we leave it as its default value.
                        // And the default value has been set in the 'Client.PostLogoutRedirectUris' in the 'Config.cs'. 
                        // options.SignedOutCallbackPath = new PathString("..."); 

                        // in case of 'AlwaysIncludeUserClaimsInIdToken' with default false value not configured at 'Config.cs' of the 'Harisoft.IDP', 
                        // and we need to get claims from userinfo in a separate call (GET https://localhost:44327/connect/userinfo) with access_token. 
                        // Doing so ensures that the middleware will call the user info endpoint to get additional information on the user. But user claims are not in id_token.
                        options.GetClaimsFromUserInfoEndpoint = true;

                        options.Events = new OpenIdConnectEvents
                        {
                            OnTicketReceived = ticketReceivedContext => Task.CompletedTask,

                            OnTokenValidated = tokenValidatedContext =>
                            {
                                #region Claims transformation: only keeping the claims you need.

                                var identity = tokenValidatedContext.Principal.Identity as ClaimsIdentity;

                                // Filter Claims to only including these 4 claims. 
                                var targetClaims = identity.Claims.Where(z => new[] { "subscriptionlevel", "country", "role", "sub" }.Contains(z.Type));

                                // And then create new a new claim "given_name" identity.
                                var newClaimsIdentity = new ClaimsIdentity(
                                    targetClaims,
                                    identity.AuthenticationType,
                                    "given_name", // setting which claim type to use for name.
                                    "role");      // setting which claim type to use for role. We don't have a role claim yet, but we will name its role later on.

                                tokenValidatedContext.Principal = new ClaimsPrincipal(newClaimsIdentity);
                                #endregion

                                return Task.CompletedTask;
                            },

                            OnUserInformationReceived = userInformationReceivedContext =>
                            {
                                #region Getting additional information through the userinfo endpoint

                                // The user object isn't the claims identity yet. 
                                // It's a JSON object constructed from the JSON response with the claims that are returned from the userinfo endpoint.
                                // We are removing the address before the middleware has a chance to add it to the claims identity.
                                userInformationReceivedContext.User.Remove("address");

                                #endregion

                                return Task.FromResult(0);
                            }
                        };
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
                app.UseExceptionHandler("/Shared/Error");
            }

            // Clear: default mapping dictionary that's included will claim types to their WS security standard counterparts. 
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            JwtSecurityTokenHandler.DefaultInboundClaimFilter.Clear();

            /*
             Next we need to add Authentication to our pipeline (Configure), before UseMvc.
            */
            app.UseAuthentication();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=TodoItems}/{action=Index}/{id?}");
            });
        }
    }
}
