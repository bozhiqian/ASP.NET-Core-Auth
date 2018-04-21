using System.Linq;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.EntityFrameworkCore;

namespace IdentityServer.Entities
{
    public static class ConfigurationDbContextSeeder
    {
        public static async void SeedConfigurationDataForIndentitySever(this ConfigurationDbContext context)
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
                context.SaveChanges();
            }

            if (!context.IdentityResources.Any())
            {
                await context.IdentityResources.AddRangeAsync(Config.GetIdentityResources().Select(r => r.ToEntity()));
                context.SaveChanges();
            }

            if (!context.ApiResources.Any())
            {
                await context.ApiResources.AddRangeAsync(Config.GetApiResources().Select(r => r.ToEntity()));
                context.SaveChanges();
            }
        }

        private static void Delete<T>(this ConfigurationDbContext context, DbSet<T> dbset) where T : class
        {
            foreach (var entity in dbset)
            {
                dbset.Remove(entity);
            }
            context.SaveChanges();
        }
    }
}
