using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TodoApi.Entities;

namespace TodoApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);

            // Seed the database
            using (var serviceScope = host.Services.CreateScope())
            {
                // https://github.com/aspnet/EntityFrameworkCore/issues/2874

                var serviceProvider = serviceScope.ServiceProvider;

                try
                {
                    var todoContext = serviceProvider.GetRequiredService<TodoContext>();
                    todoContext.Database.Migrate();

                    todoContext.SeedDataForTodoContext(); // Seeding data.
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
                .Build();
    }
}
