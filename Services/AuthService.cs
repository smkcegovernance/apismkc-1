using SmkcApi.Models;
using SmkcApi.Repositories;

namespace SmkcApi.Services
{
    public interface IAuthService
    {
        ApiResponse<object> BankLogin(string userId, string password);
        ApiResponse<object> AccountLogin(string userId, string password);
        ApiResponse<object> CommissionerLogin(string userId, string password);
        ApiResponse<object> UnifiedLogin(string userId, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        public AuthService(IAuthRepository repo)
        {
            _repo = repo;
        }

        public ApiResponse<object> BankLogin(string userId, string password) => _repo.Bank_Login(userId, password);
        public ApiResponse<object> AccountLogin(string userId, string password) => _repo.Account_Login(userId, password);
        public ApiResponse<object> CommissionerLogin(string userId, string password) => _repo.Commissioner_Login(userId, password);
        public ApiResponse<object> UnifiedLogin(string userId, string password) => _repo.UnifiedLogin(userId, password);
    }
}
