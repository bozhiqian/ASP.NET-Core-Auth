using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerWithAspNetIdentity.Extensions
{
    public static class DbContextExtensions
    {
        public static void Delete<T>(this DbContext context, DbSet<T> dbset) where T : class
        {
            foreach (var entity in dbset)
            {
                dbset.Remove(entity);
            }
            context.SaveChanges();
        }
    }
}
