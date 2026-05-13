using System.Collections.Generic;
using SmkcApi.Models.UserRights;

namespace SmkcApi.Repositories.UserRights
{
    public interface IUserRightsRepository
    {
        /// <summary>
        /// Returns effective rights for a user.
        /// ADMIN001 and PTTEST01 get IsAdmin=true with empty Rights list
        /// (frontend grants them access to everything).
        /// </summary>
        UserRightsDto GetUserRights(int ulbCode, string userId);

        /// <summary>
        /// Returns all ERP users from ULBERP.USERDET with their dept info
        /// and a flag indicating whether they have any custom rights set.
        /// </summary>
        IEnumerable<ErpUserInfoDto> GetAllUsers(int ulbCode);

        /// <summary>
        /// Replaces ALL rights for userId with the supplied list.
        /// Deletes existing rows then inserts the new ones in one transaction.
        /// </summary>
        void SaveUserRights(int ulbCode, string userId,
            IEnumerable<DeptRightsItemDto> rights, string adminUserId);

        /// <summary>
        /// Grants all supplied menuItems under deptKey to every userId in userIds.
        /// Skips rows that already exist (INSERT ... WHERE NOT EXISTS).
        /// </summary>
        void BulkSeedRights(int ulbCode, string deptKey,
            IEnumerable<string> userIds, IEnumerable<string> menuItems,
            string adminUserId);

        /// <summary>
        /// Removes all rights for userId (full reset).
        /// </summary>
        void ClearUserRights(int ulbCode, string userId, string adminUserId);

        /// <summary>
        /// Seeds default rights for ALL active users:
        ///   - Everyone → General Administration menus
        ///   - Accounts dept users (dept name contains "account"/"लेखा") → Accounts menus
        ///   - Audit dept users (dept name contains "audit"/"लेखापरीक्षण") → Audit menus
        /// Skips rows that already exist. Returns a summary message.
        /// </summary>
        string SeedDefaultRights(int ulbCode, string adminUserId);
    }
}
