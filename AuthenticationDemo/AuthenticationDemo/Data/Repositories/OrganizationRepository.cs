using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticationDemo.Models;

namespace AuthenticationDemo.Data.Repositories
{
    public class OrganizationRepository : GenericRepository<Organization>, IOrganizationRepository
    {
        public OrganizationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public Organization Get(string id)
        {
            var organization = GetAll().FirstOrDefault(b => b.Id == id);
            return organization;
        }

        
    }
}
