using System.Threading.Tasks;

namespace TodoWebClient.Services
{
    public interface ITodoApiHttpClient
    {
        Task<System.Net.Http.HttpClient> GetClient();
    }
}
