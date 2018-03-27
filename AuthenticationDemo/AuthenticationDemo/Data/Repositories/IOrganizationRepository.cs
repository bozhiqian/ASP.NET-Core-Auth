using AuthenticationDemo.Models;

namespace AuthenticationDemo.Data.Repositories
{
    public interface IOrganizationRepository : IGenericRepository<Organization>
    {
        Organization Get(string id);
    }
}