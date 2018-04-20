using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace IdentityServer.Entities
{
    public static class UserContextSeeder
    {
        public static void SeedUserData(this UserContext context)
        {
            #region Reset configuration data
            // for debug use only.
            // context.Delete(context.Users);

            #endregion

            if (context.Users.Any()) return;

            // init users
            var users = Config.GetUsers();

            context.Users.AddRange(users);
            context.SaveChanges();
        }

        private static void Delete<T>(this UserContext context, DbSet<T> dbset) where T : class
        {
            foreach (var entity in dbset)
            {
                dbset.Remove(entity);
            }
            context.SaveChanges();
        }
    }
}