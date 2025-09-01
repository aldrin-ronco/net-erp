using Models.Login;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface ILoginService
    {
        Task<LoginGraphQLModel> AuthenticateAsync(string email, string password);
    }
}