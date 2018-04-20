using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using IdentityServer.Entities;
using IdentityServer.Extensions;
using IdentityServer.Services;
using IdentityServer4;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace IdentityServer
{
    public class Startup
    {
        public readonly string UserConnectionString;
        public readonly string IdentityServerConnectionString;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Config.Configuration = configuration;

            UserConnectionString = Configuration["connectionStrings:userDBConnectionString"];
            IdentityServerConnectionString = Configuration["connectionStrings:identityServerDataDBConnectionString"];
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddScoped<IUserRepository, UserRepository>();

            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            // Add-migration -name InitialUserDBMigration -context UserContext 
            services.AddDbContext<UserContext>(o => o.UseSqlServer(UserConnectionString,
                    sqlOptions => { sqlOptions.MigrationsAssembly(migrationsAssembly); }));

            services.AddIdentityServer()
                    /*
                     Signing Certificate
                     A signing certificate is a dedicated certificate used to sign tokens, allowing for client applications 
                     to verify that the contents of the token have not been altered in transit. 
                     This involves a private key used to sign the token and a public key to verify the signature. 
                     This public key is accessible to client applications via the jwks_uri in the OpenID Connect discovery document. 

                     When you go to create and use your own signing certificate, feel free to use a self-signed certificate. 
                     This certificate does not need to be issued by a trusted certificate authority.
                    */
                    .AddDeveloperSigningCredential()
                    //.AddSigningCredential(LoadCertificateFromStore(Configuration["Authentication:Certificate:ThumbPrint"])) // Using local self-signed certificate.
                    .AddUserStore()

            #region Configuring the stores, refer to http://identityserver4.readthedocs.io/en/release/quickstarts/8_entity_framework.html
                     // Configures EF implementation of IClientStore, IResourceStore, and ICorsPolicyService with IdentityServer.

                     // Add-migration -name InitialIndentityServerConfigurationDBMigration -context ConfigurationDbContext 
                     // Add-migration -name InitialIndentityServerOperationalDBMigration -context PersistedGrantDbContext 
                     // dotnet ef migrations add InitialIndentityServerOperationalDBMigration -c PersistedGrantDbContext
                     // dotnet ef migrations add InitialIndentityServerConfigurationDBMigration - c ConfigurationDbContext

                     /*
                      Client and Scope stores
                      These registrations also include a CORS policy service that reads from our Client tables.
                     */
                     .AddConfigurationStore(options =>
                    {
                        options.ConfigureDbContext = context =>
                            context.UseSqlServer(IdentityServerConnectionString,
                                sqlOptions => { sqlOptions.MigrationsAssembly(migrationsAssembly); });
                    })
                    /*
                     Persisted grant store
                     The persisted grant store contains all information regarding given consent (so we don't keep asking for consent on every request), 
                     reference tokens (stored jwt’s where only a key corresponding to the jwt is given to the requester, making them easily revocable), 
                     and much more. 
                     Without a persistent store for this, tokens will be invalidated on every redeploy of IdentityServer and we wouldn't be able to host 
                     more than one installation at a time (no load balancing).
                    */
                    .AddOperationalStore(options =>
                    {
                        options.ConfigureDbContext = context =>
                            context.UseSqlServer(IdentityServerConnectionString,
                                sqlOptions => { sqlOptions.MigrationsAssembly(migrationsAssembly); });
                    });
            #endregion

            services.AddAuthentication()

            #region External authentication

                // Adding support for external authentication https://github.com/IdentityServer/IdentityServer4.Quickstart.UI
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?tabs=aspnetcore2x
                // https://console.developers.google.com/projectselector/apis/library
                .AddGoogle("Google", googleOptions =>
                {
                    /*
                    Note for Google authentication you need to register your local quickstart identityserver using the Google developer console at https://console.developers.google.com/. 
                    As a redirect URL, use the URL of your local identityserver and add /signin-google. 
                    If your IdentityServer is running on port 5000 - you can use the above client id/secret which is pre-registered.
                    */
                    googleOptions.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    googleOptions.ClientId = Configuration["Authentication:Google:ClientId"];
                    googleOptions.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                })
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins?tabs=aspnetcore2x
                // https://apps.dev.microsoft.com
                .AddMicrosoftAccount(microsoftOptions =>
                {
                    microsoftOptions.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    microsoftOptions.ClientId = Configuration["Authentication:Microsoft:ApplicationId"];
                    microsoftOptions.ClientSecret = Configuration["Authentication:Microsoft:Password"];
                })
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/twitter-logins?tabs=aspnetcore2x
                // https://apps.twitter.com/
                // https://help.twitter.com/forms/platform
                .AddTwitter(twitterOptions =>
                {
                    twitterOptions.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    twitterOptions.ConsumerKey = Configuration["Authentication:Twitter:ConsumerKey"];
                    twitterOptions.ConsumerSecret = Configuration["Authentication:Twitter:ConsumerSecret"];

                    // Refer to 'Get the user's e-mail address from Twitter' at https://github.com/aspnet/Security/issues/765
                    twitterOptions.Events = new TwitterEvents()
                    {
                        OnCreatingTicket = async context =>
                        {
                            var nonce = Guid.NewGuid().ToString("N");

                            var authorizationParts = new SortedDictionary<string, string>
                            {
                                {"oauth_consumer_key", context.Options.ConsumerKey},
                                {"oauth_nonce", nonce},
                                {"oauth_signature_method", "HMAC-SHA1"},
                                {"oauth_timestamp", GenerateTimeStamp()},
                                {"oauth_token", context.AccessToken},
                                {"oauth_version", "1.0"}
                            };

                            var parameterBuilder = new StringBuilder();
                            foreach (var authorizationKey in authorizationParts)
                            {
                                parameterBuilder.AppendFormat("{0}={1}&",
                                    UrlEncoder.Default.Encode(authorizationKey.Key),
                                    UrlEncoder.Default.Encode(authorizationKey.Value));
                            }

                            parameterBuilder.Length--;
                            var parameterString = parameterBuilder.ToString();

                            var resource_url = "https://api.twitter.com/1.1/account/verify_credentials.json";
                            var resource_query = "include_email=true";
                            var canonicalizedRequestBuilder = new StringBuilder();
                            canonicalizedRequestBuilder.Append(HttpMethod.Get.Method);
                            canonicalizedRequestBuilder.Append("&");
                            canonicalizedRequestBuilder.Append(UrlEncoder.Default.Encode(resource_url));
                            canonicalizedRequestBuilder.Append("&");
                            canonicalizedRequestBuilder.Append(UrlEncoder.Default.Encode(resource_query));
                            canonicalizedRequestBuilder.Append("%26");
                            canonicalizedRequestBuilder.Append(UrlEncoder.Default.Encode(parameterString));

                            var signature = ComputeSignature(context.Options.ConsumerSecret, context.AccessTokenSecret, canonicalizedRequestBuilder.ToString());
                            authorizationParts.Add("oauth_signature", signature);

                            var authorizationHeaderBuilder = new StringBuilder();
                            authorizationHeaderBuilder.Append("OAuth ");
                            foreach (var authorizationPart in authorizationParts)
                            {
                                authorizationHeaderBuilder.AppendFormat(
                                    "{0}=\"{1}\", ", authorizationPart.Key,
                                    UrlEncoder.Default.Encode(authorizationPart.Value));
                            }

                            authorizationHeaderBuilder.Length = authorizationHeaderBuilder.Length - 2;

                            var request = new HttpRequestMessage(HttpMethod.Get, resource_url + "?include_email=true");
                            request.Headers.Add("Authorization", authorizationHeaderBuilder.ToString());

                            var httpClient = new System.Net.Http.HttpClient();
                            var response = await httpClient.SendAsync(request, context.HttpContext.RequestAborted);
                            response.EnsureSuccessStatusCode();
                            string responseText = await response.Content.ReadAsStringAsync();

                            var result = JObject.Parse(responseText);

                            var email = result.Value<string>("email");
                            var identity = (ClaimsIdentity)context.Principal.Identity;
                            if (!string.IsNullOrEmpty(email))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Email, email, ClaimValueTypes.String, "Twitter"));
                            }
                        }
                    };
                })
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/facebook-logins?tabs=aspnetcore2x
                // https://developers.facebook.com/apps/ Need to set 'Valid OAuth Redirect URIs' to this IdentityServer4 url.
                .AddFacebook(FacebookDefaults.AuthenticationScheme, FacebookDefaults.DisplayName, facebookOptions =>
                {
                    // What we are setting the SignInScheme value to is the name of the cookie middleware that will temporarily
                    // store the outcome of the external authentication.
                    // IdentityServer by default registers cookies middleware with an idsrv.external as scheme name.
                    facebookOptions.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme; // idsrv.external 

                    facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
                    facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];

                    // Optional: 
                    //facebookOptions.Scope.Add("emailaddress");
                    //facebookOptions.Fields.Add("name");
                    //facebookOptions.Fields.Add("email");
                    //facebookOptions.SaveTokens = true;
                })
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/cookie?tabs=aspnetcore2x
                .AddCookie("idsrv.2FA", configureOptions => { }); // 2-factor Authentication.

            #endregion

            // Register application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();
            services.Configure<AuthMessageSMSSenderOptions>(options =>
                {
                    options.SID = Configuration["Authentication:Twilio:SID"];
                    options.AuthToken = Configuration["Authentication:Twilio:AuthToken"];
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                loggerFactory.AddConsole();
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseIdentityServer();
            app.UseAuthentication();

            app.UseMvcWithDefaultRoute();
        }

        public X509Certificate2 LoadCertificateFromStore(string thumbPrint)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbPrint, true);
                if (certCollection.Count == 0)
                {
                    throw new Exception("The specified certificate wasn't found. Check the specified thumbprint.");
                }

                return certCollection[0];
            }
        }

        private string GenerateTimeStamp()
        {
            var secondsSinceUnixEpocStart = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64(secondsSinceUnixEpocStart.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        private string ComputeSignature(string consumerSecret, string tokenSecret, string signatureData)
        {
            using (var algorithm = new HMACSHA1())
            {
                algorithm.Key = Encoding.ASCII.GetBytes(
                    string.Format(CultureInfo.InvariantCulture,
                        "{0}&{1}",
                        UrlEncoder.Default.Encode(consumerSecret),
                        string.IsNullOrEmpty(tokenSecret) ? string.Empty : UrlEncoder.Default.Encode(tokenSecret)));
                var hash = algorithm.ComputeHash(Encoding.ASCII.GetBytes(signatureData));
                return Convert.ToBase64String(hash);
            }
        }
    }
}
// Quickstart UI for IdentityServer4 v2 https://github.com/IdentityServer/IdentityServer4.Quickstart.UI
