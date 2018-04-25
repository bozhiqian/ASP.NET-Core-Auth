using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerWithAspNetIdentity.Data
{
    public static class ApplicationDbContextSeed
    {
        public static async Task SeedAsync(this ApplicationDbContext context)
        {
            try
            {
                #region Reset configuration data
                // for debug use only.
                // context.Delete(context.Users);

                #endregion

                if (!context.Users.Any())
                {
                    context.Users.AddRange(Config.GetUsers());

                    await context.SaveChangesAsync();
                }

            }
            catch (Exception ex)
            {
                //logger.LogError(ex.Message, $"There is an error migrating data for ApplicationDbContext");
            }
        }

        private static void Delete<T>(this ApplicationDbContext context, DbSet<T> dbset) where T : class
        {
            foreach (var entity in dbset)
            {
                dbset.Remove(entity);
            }
            context.SaveChanges();
        }
    }
}
