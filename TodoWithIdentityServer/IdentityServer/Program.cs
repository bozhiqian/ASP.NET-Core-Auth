using System;
using IdentityServer.Entities;
using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace IdentityServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "IdentityServer4";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Literate)
                .CreateLogger();

            var host = BuildWebHost(args);

            // Seed the database
            using (var serviceScope = host.Services.CreateScope())
            {
                // https://github.com/aspnet/EntityFrameworkCore/issues/2874

                var serviceProvider = serviceScope.ServiceProvider;

                try
                {
                    // this is AddOperationalStore
                    var persistedGrantDbContext = serviceProvider.GetRequiredService<PersistedGrantDbContext>();
                    persistedGrantDbContext.Database.Migrate();

                    // This is AddConfigurationStore
                    var configurationDbContext = serviceProvider.GetRequiredService<ConfigurationDbContext>();
                    configurationDbContext.Database.Migrate();

                    // This is AddUserStore
                    var userContext = serviceProvider.GetRequiredService<UserContext>();
                    userContext.Database.Migrate();

                    userContext.SeedUserData(); // Seeding user data.
                    configurationDbContext.SeedConfigurationDataForIndentitySever(); // Seeding identityServer configuration data.
                }
                catch (Exception ex)
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred seeding the DB.");
                }
            }

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                })
                .Build();
    }
}
