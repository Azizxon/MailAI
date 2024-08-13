using System.Net.Http;
using System.Threading.Tasks;
using Shared.Models;

namespace Shared.Services.Authentication
{
    public interface IAuthenticationClient
    {
        Task<Token?> GetAccessTokenAsync();
        Task<Token?> SendRequestAsync(HttpRequestMessage request);
    }
}
