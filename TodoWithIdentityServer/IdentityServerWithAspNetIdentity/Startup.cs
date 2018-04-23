using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdentityServerWithAspNetIdentity.Data;
using IdentityServerWithAspNetIdentity.Models;
using IdentityServerWithAspNetIdentity.Services;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Twitter;
using Newtonsoft.Json.Linq;

namespace IdentityServerWithAspNetIdentity
{
    public class Startup
    {
        public readonly string UserConnectionString;
        public readonly string IdentityServerConnectionString;
        public readonly string ExternalScheme;
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Config.Configuration = configuration;
            Environment = env;

            UserConnectionString = Configuration.GetConnectionString("userDBConnectionString"); // Configuration["connectionStrings:userDBConnectionString"];
            IdentityServerConnectionString = Configuration["connectionStrings:identityServerDataDBConnectionString"];
            ExternalScheme = Config.ExternalScheme;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            // Add-migration -name InitialUserDBMigration -context ApplicationDbContext 
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(UserConnectionString, sqlOptions => { sqlOptions.MigrationsAssembly(migrationsAssembly); }));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();

            // configure identity server with in-memory stores, keys, clients and scopes
            var builder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                })
                //.AddDeveloperSigningCredential()
                //.AddInMemoryPersistedGrants()
                //.AddInMemoryIdentityResources(Config.GetIdentityResources())
                //.AddInMemoryApiResources(Config.GetApiResources())
                //.AddInMemoryClients(Config.GetClients())

                /*
                 It’s important when using ASP.NET Identity that IdentityServer be registered after ASP.NET Identity in the DI system 
                 because IdentityServer is overwriting some configuration from ASP.NET Identity.
                 */
                .AddAspNetIdentity<ApplicationUser>()
            #region Configuring the stores, refer to http://identityserver4.readthedocs.io/en/release/quickstarts/8_entity_framework.html
                     // Configures EF implementation of IClientStore, IResourceStore, and ICorsPolicyService with IdentityServer.

                     // Add-migration -name InitialIndentityServerConfigurationDBMigration -context ConfigurationDbContext 
                     // Add-migration -name InitialIndentityServerOperationalDBMigration -context PersistedGrantDbContext 
                     // dotnet ef migrations add InitialIndentityServerOperationalDBMigration -c PersistedGrantDbContext
                     // dotnet ef migrations add InitialIndentityServerConfigurationDBMigration - c ConfigurationDbContext

                     // Update-Database -Context ConfigurationDbContext
                     // Update-Database -Context PersistedGrantDbContext

                     /*
                      Client and Scope stores -- // this adds the config data from DB (clients, resources)
                      These registrations also include a CORS policy service that reads from our Client tables.
                     */
                     .AddConfigurationStore(options =>
                     {
                         options.ConfigureDbContext = context =>
                             context.UseSqlServer(IdentityServerConnectionString,
                                 sqlOptions => { sqlOptions.MigrationsAssembly(migrationsAssembly); });
                     })
                    /*
                     Persisted grant store -- // this adds the operational data from DB (codes, tokens, consents)
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

                        // this enables automatic token cleanup. this is optional.
                        options.EnableTokenCleanup = true;
                        // options.TokenCleanupInterval = 15; // frequency in seconds to cleanup stale grants. 15 is useful during debugging
                    });
            #endregion

            if (Environment.IsDevelopment())
            {
                builder.AddDeveloperSigningCredential();
            }
            else
            {
                throw new Exception("need to configure key material");
            }

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
                        googleOptions.SignInScheme = ExternalScheme;
                        googleOptions.ClientId = Configuration["Authentication:Google:ClientId"];
                        googleOptions.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                    })
                    // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins?tabs=aspnetcore2x
                    // https://apps.dev.microsoft.com
                    .AddMicrosoftAccount(microsoftOptions =>
                    {
                        microsoftOptions.SignInScheme = ExternalScheme;
                        microsoftOptions.ClientId = Configuration["Authentication:Microsoft:ApplicationId"];
                        microsoftOptions.ClientSecret = Configuration["Authentication:Microsoft:Password"];
                    })
                    // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/twitter-logins?tabs=aspnetcore2x
                    // https://apps.twitter.com/
                    // https://help.twitter.com/forms/platform
                    .AddTwitter(twitterOptions =>
                    {
                        twitterOptions.SignInScheme = ExternalScheme;
                        twitterOptions.ConsumerKey = Configuration["Authentication:Twitter:ConsumerKey"];
                        twitterOptions.ConsumerSecret = Configuration["Authentication:Twitter:ConsumerSecret"];

                        // Refer to 'Get the user's e-mail address from Twitter' at https://github.com/aspnet/Security/issues/765
                        twitterOptions.Events = new TwitterEvents()
                        {
                            OnCreatingTicket = OnCreatingTicket
                        };
                    })
                    // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/facebook-logins?tabs=aspnetcore2x
                    // https://developers.facebook.com/apps/ Need to set 'Valid OAuth Redirect URIs' to this IdentityServer4 url.
                    .AddFacebook(FacebookDefaults.AuthenticationScheme, FacebookDefaults.DisplayName, facebookOptions =>
                    {
                        // What we are setting the SignInScheme value to is the name of the cookie middleware that will temporarily
                        // store the outcome of the external authentication.
                        // IdentityServer by default registers cookies middleware with an idsrv.external as scheme name.
                        facebookOptions.SignInScheme = ExternalScheme; // idsrv.external 

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

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            // app.UseAuthentication(); // not needed, since UseIdentityServer adds the authentication middleware
            app.UseIdentityServer();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        #region Helper methods

        public async Task OnCreatingTicket(TwitterCreatingTicketContext context)
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
        #endregion

    }
}
