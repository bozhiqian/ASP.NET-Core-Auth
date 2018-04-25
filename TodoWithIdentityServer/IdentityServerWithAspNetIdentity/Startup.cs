using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdentityServerWithAspNetIdentity.Data;
using IdentityServerWithAspNetIdentity.Extensions;
using IdentityServerWithAspNetIdentity.Models;
using IdentityServerWithAspNetIdentity.Services;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Twitter;

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
                .AddSigningCredential(Certificate.Certificate.Get(Configuration["Authentication:Certificate:ThumbPrint"])) // Using local self-signed certificate.
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
                                 sqlOptions =>
                                 {
                                     sqlOptions.MigrationsAssembly(migrationsAssembly);

                                     //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                                     sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                                 });
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
                                sqlOptions =>
                                {
                                    sqlOptions.MigrationsAssembly(migrationsAssembly);

                                    //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                                });

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
                            OnCreatingTicket = async context => await context.CreatingTicket()
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
    }
}
