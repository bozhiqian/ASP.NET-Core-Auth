using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticationDemo.Claims;
using AuthenticationDemo.CustomTokenProvider;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AuthenticationDemo.Data;
using AuthenticationDemo.Data.Repositories;
using AuthenticationDemo.Models;
using AuthenticationDemo.Services;
using Microsoft.AspNetCore.Authentication;

namespace AuthenticationDemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // https://github.com/aspnet/Identity/blob/329eed9e8d14243d0b36385bb1adc9fc85df0e41/src/Identity/IdentityServiceCollectionExtensions.cs#L47
            services.AddIdentity<ApplicationUser, IdentityRole>(identityOptions =>
                {
                    // Account confirmation and password recovery in ASP.NET Core
                    // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?tabs=aspnetcore2x
                    identityOptions.SignIn.RequireConfirmedEmail = true;
                    identityOptions.Tokens.EmailConfirmationTokenProvider = "EmailConfirmation";
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                // Added 4 default token provider. Ref: https://github.com/aspnet/Identity/blob/e09c72c7b44a004d0f7af73c9301bdc535b5df39/src/Identity/IdentityBuilderExtensions.cs#L28
                .AddDefaultTokenProviders()
                .AddTokenProvider<EmailConfirmationTokenProvider<ApplicationUser>>("EmailConfirmation");

            services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromDays(1));
            services.Configure<EmailConfirmationTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromDays(2));

            services.AddAuthentication(options =>
                {
                    // these are set by default at services.AddIdentity(...) from source code, no need to add them again unless need to change.
                    //options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                    //options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                    //options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                })
                /*
                 // these are set by default at services.AddIdentity(...) from source code, no need to add them again unless need to change.
                    .AddCookie(IdentityConstants.ApplicationScheme, o =>
                    {
                        o.LoginPath = new PathString("/Account/Login");
                        o.Events = new CookieAuthenticationEvents
                        {
                            OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync
                        };
                    })
                    .AddCookie(IdentityConstants.ExternalScheme, o =>
                    {
                        o.Cookie.Name = IdentityConstants.ExternalScheme;
                        o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                    })
                    .AddCookie(IdentityConstants.TwoFactorRememberMeScheme, o =>
                    {
                        o.Cookie.Name = IdentityConstants.TwoFactorRememberMeScheme;
                        o.Events = new CookieAuthenticationEvents
                        {
                            OnValidatePrincipal = SecurityStampValidator.ValidateAsync<ITwoFactorSecurityStampValidator>
                        };
                    })
                    .AddCookie(IdentityConstants.TwoFactorUserIdScheme, o =>
                    {
                        o.Cookie.Name = IdentityConstants.TwoFactorUserIdScheme;
                        o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                    });
                */
                
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?tabs=aspnetcore2x
                .AddGoogle(googleOptions =>
                {
                    googleOptions.ClientId = Configuration["Authentication:Google:ClientId"];
                    googleOptions.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                    // googleOptions.SignInScheme = IdentityConstants.ExternalScheme; // it does not need as this is default in IdentityServiceCollectionExtensions
                })
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/facebook-logins?tabs=aspnetcore2x
                .AddFacebook(facebookOptions =>
                {
                    facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
                    facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
                })
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/twitter-logins?tabs=aspnetcore2x
                .AddTwitter(twitterOptions =>
                {
                    twitterOptions.ConsumerKey = Configuration["Authentication:Twitter:ConsumerKey"];
                    twitterOptions.ConsumerSecret = Configuration["Authentication:Twitter:ConsumerSecret"];
                })
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins?tabs=aspnetcore2x
                .AddMicrosoftAccount(microsoftOptions =>
                {
                    microsoftOptions.ClientId = Configuration["Authentication:Microsoft:ApplicationId"];
                    microsoftOptions.ClientSecret = Configuration["Authentication:Microsoft:Password"];
                });
            // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/other-logins

            /*
                 // if you need to overwrit the above cookie configuration from IdentityConstants.ApplicationScheme, you can add:
                 services.ConfigureApplicationCookie(options => options.ExpireTimeSpan = TimeSpan.FromHours(1));
            */


            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
            services.AddScoped<ClaimsHelper>();
            // services.AddScoped<IClaimsTransformation, ApplicationUserClaimsTransformation>();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddTransient<IOrganizationRepository, OrganizationRepository>();

            services.AddMvc();
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

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
