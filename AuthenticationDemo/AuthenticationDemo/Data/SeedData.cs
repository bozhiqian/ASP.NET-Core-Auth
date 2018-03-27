using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticationDemo.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AuthenticationDemo.Data
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();

            if (!context.Set<Organization>().Any())
            {
                context.Set<Organization>().Add(new Organization() { Name = "Contoso", Id = Guid.NewGuid().ToString()});
                context.SaveChanges();
            }
        }
    }
}
