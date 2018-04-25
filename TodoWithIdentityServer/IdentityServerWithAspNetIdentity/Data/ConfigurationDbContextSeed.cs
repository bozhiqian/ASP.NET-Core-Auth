using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServerWithAspNetIdentity.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IdentityServerWithAspNetIdentity.Data
{
    public static class ConfigurationDbContextSeed
    {
        public static async Task SeedAsync(this ConfigurationDbContext context)
        {
            #region Reset configuration data
            // for debug use only.
            //context.Delete(context.Clients);
            //context.Delete(context.IdentityResources);
            //context.Delete(context.ApiResources);

            #endregion

            if (!context.Clients.Any())
            {
                await context.Clients.AddRangeAsync(Config.GetClients().Select(c => c.ToEntity()));
                await context.SaveChangesAsync();
            }

            if (!context.IdentityResources.Any())
            {
                await context.IdentityResources.AddRangeAsync(Config.GetIdentityResources().Select(r => r.ToEntity()));
                await context.SaveChangesAsync();
            }

            if (!context.ApiResources.Any())
            {
                await context.ApiResources.AddRangeAsync(Config.GetApiResources().Select(r => r.ToEntity()));
                await context.SaveChangesAsync();
            }
        }
    }
}
