using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TodoApi.Authorization;
using TodoApi.Entities;
using TodoApi.Services;
using TodoViewModel;

namespace TodoApi
{
    public class Startup
    {
        public readonly string Authority; // Identity Provider: IdentityServer4 Url.
        public readonly string ApiName;
        public readonly string ApiSecret;
        public readonly string ConnectionString; // TodoDb sql connection string.

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            Authority = Configuration["Authority:IdentityServer4:Url"];
            ApiName = Configuration["Authority:IdentityServer4:ApiName"];
            ApiSecret = Configuration["Authority:IdentityServer4:ApiSecret"];
            ConnectionString = Configuration["connectionStrings:TodoDBConnectionString"];
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddAuthorization(options =>
            {
                // Attribute-based Access Control(ABAC)
                options.AddPolicy("MustOwnTodoItem", policybuilder =>
                {
                    policybuilder.RequireAuthenticatedUser();

                    // Extending Authorization Policies with Requirements and Handlers
                    policybuilder.Requirements = new IAuthorizationRequirement[] { new MustOwnTodoItemRequirement() };
                });
            });

            services.AddScoped<IAuthorizationHandler, MustOwnTodoItemHandler>();

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                // Securing Access to Your API - Registers the IdentityServer authentication handler.
                .AddIdentityServerAuthentication(options => // need to install nuget package "IdentityServer4.AccessTokenValidation".
                {
                    options.Authority = Authority; //"https://localhost:44327"; // this is the address of our IndentityServer(IDP).

                    // When this middleware is hit for the first time, it will read the metadata from the identity provider, 
                    // and it's also responsible for validating the access tokens. It does that locally, so there's no continuous commmnunication with the IDP.
                    options.RequireHttpsMetadata = true;

                    // this makes sure that this middleware checks for this value in the access token. 
                    options.ApiName = ApiName; //"TodoApi"; // the ApiResource "TodoApi" is set in GetApiResources() of 'Config.cs' in the IdentityServer project.

                    options.ApiSecret = ApiSecret; //"apisecret"; // this should be the same as defined in 'Config.cs' at ApiSecrets. 
                });

            // Add-migration -name InitialMigration -context TodoContext 
            services.AddDbContext<TodoContext>(o => o.UseSqlServer(ConnectionString));

            // register the repository
            services.AddScoped<ITodoRepository, TodoRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        // ensure generic 500 status code on fault.
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unexpected fault happened. Try again later.");
                    });
                });
            }

            app.UseStaticFiles();

            AutoMapper.Mapper.Initialize(cfg =>
            {
                // Map from TodoItem (entity) to TodoItemViewModel, and back
                cfg.CreateMap<TodoItem, TodoItemViewModel>().ReverseMap();

                // Map from TodoItemForCreationViewModel to TodoItem, Ignore properties that shouldn't be mapped
                cfg.CreateMap<TodoItemForCreationViewModel, TodoItem>()
                    .ForMember(m => m.Id, options => options.Ignore())
                    .ForMember(m => m.IsComplete, options => options.Ignore())
                    .ForMember(m => m.OwnerId, options => options.Ignore());

                // Map from TodoItemForUpdateViewModel to TodoItem, ignore properties that shouldn't be mapped
                cfg.CreateMap<TodoItemForUpdateViewModel, TodoItem>()
                    .ForMember(m => m.Id, options => options.Ignore())
                    .ForMember(m => m.OwnerId, options => options.Ignore());
            });

            AutoMapper.Mapper.AssertConfigurationIsValid();

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
