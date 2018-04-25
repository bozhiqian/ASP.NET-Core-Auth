using IdentityServer4.EntityFramework.DbContexts;
using IdentityServerWithAspNetIdentity.Data;
using IdentityServerWithAspNetIdentity.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;


namespace IdentityServerWithAspNetIdentity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args)
                // Seed the database
                .MigrateDbContext<ApplicationDbContext>(context => context.SeedAsync().Wait())
                .MigrateDbContext<ConfigurationDbContext>(context => context.SeedAsync().Wait())
                .MigrateDbContext<PersistedGrantDbContext>()
                .Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
