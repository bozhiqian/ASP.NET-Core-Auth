using System.Collections.Generic;
using IdentityServer.Entities;
using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;

namespace IdentityServer
{
    public static class Config
    {
        public static IConfiguration Configuration { get; set; }

        public static List<User> GetUsers()
        {
            /*
             Users
             A users subject (or sub) claim is their unique identifier. This should be something unique to your identity provider, 
             not something like an email address. I point this out due to a recent vulnerability with Azure AD at http://www.thread-safe.com/2016/05/azure-ad-security-issue.html.
            */

            // init users
            var users = new List<User>
            {
                new User
                {
                    SubjectId = "d860efca-22d9-47fd-8249-791ba61b07c7",
                    Username = "Frank",
                    Password = "password",
                    IsActive = true,
                    Claims =
                    {
                        new UserClaim("role", "FreeUser"),
                        new UserClaim("given_name", "Frank"),
                        new UserClaim("family_name", "Underwood"),
                        new UserClaim("address", "Main Road 1"),
                        new UserClaim("subscriptionlevel", "FreeUser"),
                        new UserClaim("country", "nl")
                    }
                },
                new User
                {
                    SubjectId = "b7539694-97e7-4dfe-84da-b4256e1ff5c7",
                    Username = "Claire",
                    Password = "password",
                    IsActive = true,
                    Claims =
                    {
                        new UserClaim("role", "PayingUser"),
                        new UserClaim("given_name", "Claire"),
                        new UserClaim("family_name", "Underwood"),
                        new UserClaim("address", "Big Street 2"),
                        new UserClaim("subscriptionlevel", "PayingUser"),
                        new UserClaim("country", "be")
                    }
                }
            };
            return users;
        }

        #region Resources & Scopes

        /// <summary>
        /// Resources & Scopes
        /// Scopes represent what you are allowed to do. They represent the scoped access. 
        /// In IdentityServer 4 scopes are modelled as resources, which come in two flavors: Identity and API. 
        /// An identity resource allows you to model a scope that will return a certain set of claims, 
        /// whilst an API resource scope allows you to model access to a protected resource (typically an API).
        /// </summary>


        internal static IEnumerable<ApiResource> GetApiResources() // this is to be used to securing web api.
        {
            var apiSecret = Configuration["ApiResource:ApiSecret"]; // "apisecret"
            /*
             ApiResources
             For api resources we are modelling a single API that we wish to protect called TodoApi. This API has one scope that can be requested: role.

             Scope vs Resource
             OpenID Connect and OAuth scopes now being modelled as resources,
             The offline_access scope, used to request refresh tokens, is now supported by default with authorization to use this scope controlled by the Client property AllowOfflineAccess.
            */
            return new List<ApiResource>
                   {
                       new ApiResource("TodoApi", "Todo API", new[] {"role"}) // Including Identity Claims in an Access Token - The claim "role" will be included in the access token. 
                       {
                           // we must define that secret on the API resource when AccessTokenType to set to 'AccessTokenType.Reference' in GetClients(). 
                           // And this also needs to be referenced at 'options.ApiSecret = "apisecret";' in the 'Startup' of API project. 
                           ApiSecrets = new[] {new Secret(apiSecret.Sha256())}
                       },
                       // 1) and then 'TodoApi' needs to be added to 'AllowedScopes' in the given Client.
                       // 2) and .AddInMemoryApiResources(Config.GetApiResources()) needs to be added to 'Startup.cs' in the IdentityServer.
                       // 3) and 'TodoApi' needs to be added to the Scopes of OpenIdConnect in the web client 'Startup.cs'.
                   };
        }

        public static List<IdentityResource> GetIdentityResources()
        {
            /*
             IdentityResources
             The first three identity resources represent some standard OpenID Connect defined scopes we wish IdentityServer to support. 
             For example the email scope allows the email and email_verified claims to be returned. 
             We are also creating a custom identity resource in the form of role which returns an role claims for authenticated user.

             A quick tip, the openid scope is always required when using OpenID Connect flows. You can find more information about these in 
             the OpenID Connect Specification. https://openid.net/specs/openid-connect-core-1_0.html#ScopeClaims
            */
            return new List<IdentityResource>
                   {
                       // Each scope needs to be included in AllowedScopes at given Client. 
                       new IdentityResources.OpenId(),
                       new IdentityResources.Profile(),
                       new IdentityResources.Address(),

                       // These scopes are not one of the standard defined OpenID Connect scopes, so we have to add these new identity resources.
                       new IdentityResource("courses", "Your course(s)", new[] {"course"}),
                       new IdentityResource("roles", "Your role(s)", new[] {"role"}),
                       new IdentityResource("subscriptionlevel", "Your subscription level", new[] {"subscriptionlevel"}),
                       new IdentityResource("country", "Your country", new[] {"country"})
                       // after the above new scopes to be added, please add them to the list of AllowedScopes in the given Client below. 
                   };
        }
        #endregion

        /// <summary>
        /// IdentityServer needs to know what client applications are allowed to use it. I like to think of this as a whitelist, 
        /// your Access Control List. Each client application is then configured to only be allowed to do certain things, 
        /// for instance they can only ask for tokens to be returned to certain URLs, or they can only request certain information. 
        /// They have scoped access.
        /// </summary>
        public static List<Client> GetClients()
        {
            /*
             OpenID Connect
             To demonstrate authentication using OpenID Connect we’ll need to create ourselves a client web application and add a corresponding client within IdentityServer.
             First we’ll need to add a new client within IdentityServer, Where the redirect and post logout redirect uris are the url of our upcoming application. 
             The redirect uri requires the path /signin-oidc and this path will be automatically created and handled by an upcoming piece of middleware..
            */

            var client = new List<Client>
                   {
                       new Client
                       {
                           /*
                            Here we are adding a client that uses the Client Credentials OAuth grant type. This grant type requires a client Id and client secret to authorize access, 
                            with the secret being simply hashed using an extension method provided by Identity Server (we never store any passwords in plain text after all, 
                            and this is better than nothing). 
                            
                            The allowed scopes is a list of scopes that this client is allowed to request. 
                           */
                           ClientName = Configuration["Client:ClientName"], // "TodoWeb",
                           ClientId = Configuration["Client:ClientId"], // "TodoWebClient",
                           AllowedGrantTypes = GrantTypes.Hybrid,

                           // AccessTokenType - Specifies whether the access token is a reference token or a self contained JWT token (defaults to Jwt).
                           AccessTokenType = AccessTokenType.Reference, // Reference Tokens http://docs.identityserver.io/en/release/topics/reference_tokens.html

                           #region Token lifetimes and expiration (Check 'ImageGalleryHttpClient' in web client on how to refresh token.)
                           //IdentityTokenLifetime = 300, // Default is 300 seconds (5 mins). 

                           // The authorization code is exchanged for one or more tokens when the token endpoint is called. 
                           // and that's something that happens during the hybrid flow. This also warrants a low lifetime as we don't want to allow using this code for longer than required.
                           //AuthorizationCodeLifetime = 300, // Default is 300 seconds (5 mins). 

                           //AccessTokenLifetime = 3600, // Default is 1 hour.


                           #region Gaining long-lived access with Refresh Tokens

                           /*
                            A refresh token is a credential to get new tokens. Tokens are refreshed via the token endpoint. A client must authenticate itself when refreshing tokens.
                            In the request body, the refresh token is passed in as is gram type refresh token.
                            The identity provider will then validate the refresh token and respond to the new access token and optionally, 
                            a new refresh token so we can refresh again later on. 
                            Refresh token don't have to have an absolute lifetime.

                            To allow requesting a refresh token in the first place, we should allow the offline access code. 
                            This gives us access to our applications and resources, even when we are offline.
                            what offline means in this context is that the user is not logged into the identity provider anymore.
                           */
                           // AbsoluteRefreshTokenLifetime = 30 * 24 * 60 * 60, // Default is 30 days. 
                           // RefreshTokenExpiration = TokenExpiration.Absolute, // Default is Absolute.

                           // RefreshTokenExpiration = TokenExpiration.Sliding, // Once a new refresh token is requested, its lifetime will be renewed. And it's renewed by the amount of time specified.
                           // SlidingRefreshTokenLifetime =  // And it's renewed by the amount of time specified by the sliding refresh token lifetime property. 
                           #endregion

                           #endregion

                           RequireConsent = true, // Specifies whether a consent screen is required. Defaults to true. http://docs.identityserver.io/en/release/reference/client.html#refclient

                           /*
                            Imagine the case where one of the users claims is changed, say the address.
                            By default, the claims in the access token stay as is when refreshing them. 
                            So if a refresh token has a value of 30 days, in a worst case scenario, those changes won't be reflected in the access token for 30 days. 
                            By setting the UpdateAccessTokenClaimsOnRefresh to true, they will be refreshed.
                           */
                           UpdateAccessTokenClaimsOnRefresh = true,

                           /*
                            Specifies whether this client can request refresh tokens (be requesting the offline_access scope)

                            If you ask for offline_access - the consent screen will always get shown. 
                            This is to make sure the user is aware that the app is asking for long time access to resources. 
                            If the app asks for offline access, which results in a refresh token, there is no reason to ask for it again any time soon.
                            */
                           AllowOfflineAccess = true,// We have to ensure that this client is allowed offline access. 
                           // and then, open the startup class at web client level, add options.Scope.Add("offline_access");

                           RedirectUris = new List<string>
                                          {
                                              Configuration["Client:RedirectUri"] // "https://localhost:44343/signin-oidc"  // the client URI. 'signin-oidc' represent for using OpenId Connect.
                                          },
                           PostLogoutRedirectUris = {Configuration["Client:PostLogoutRedirectUri"]}, // {"https://localhost:44343/signout-callback-oidc"}, // After sign out from IDP, this is redirect to the Logout page of IdendityServer. 

                           AllowedScopes =
                           {
                               IdentityServerConstants.StandardScopes.OpenId,
                               IdentityServerConstants.StandardScopes.Profile,
                               IdentityServerConstants.StandardScopes.Address,
                               "roles",
                               "courses",
                               "TodoApi",
                               "subscriptionlevel",
                               "country"
                               // These scopes can be added when needed at the 'AddOpenIdConnect' in'Startup.cs' of web client.  
                           },
                           ClientSecrets = new List<Secret>
                                           {
                                               new Secret(Configuration["Client:ClientSecret"].Sha256()) //  new Secret("ItsMySecret".Sha256())
                                           },

                           // By default, identity server will not include the claims in the identity token.
                           // When requesting both an id token and access token, should the user claims always be added to the id token instead of requring the client to use the userinfo endpoint. Defaults to false.
                           // AlwaysIncludeUserClaimsInIdToken = true // Comment this in case of getting user claims when needed.
                       }
                   };

            return client;
        }
    }
}

