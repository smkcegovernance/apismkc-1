using SmkcApi.Models;
using SmkcApi.Repositories;

namespace SmkcApi.Services
{
    public interface IErpAuthService
    {
        ApiResponse<object> ErpLogin(string userId, string password, string ipAddress);
        ApiResponse<object> ErpGetProfile(string userId);
        ApiResponse<object> ErpChangePassword(string userId, string oldPassword, string newPassword);
        ApiResponse<object> ErpGetLockedUsers(string adminUserId);
        ApiResponse<object> ErpUnlockUser(string adminUserId, string targetUserId);
    }

    public class ErpAuthService : IErpAuthService
    {
        private readonly IErpAuthRepository _repo;

        public ErpAuthService(IErpAuthRepository repo)
        {
            _repo = repo;
        }

        public ApiResponse<object> ErpLogin(string userId, string password, string ipAddress)
            => _repo.ErpLogin(userId, password, ipAddress);

        public ApiResponse<object> ErpGetProfile(string userId)
            => _repo.ErpGetProfile(userId);

        public ApiResponse<object> ErpChangePassword(string userId, string oldPassword, string newPassword)
            => _repo.ErpChangePassword(userId, oldPassword, newPassword);

        public ApiResponse<object> ErpGetLockedUsers(string adminUserId)
            => _repo.ErpGetLockedUsers(adminUserId);

        public ApiResponse<object> ErpUnlockUser(string adminUserId, string targetUserId)
            => _repo.ErpUnlockUser(adminUserId, targetUserId);
    }
}
