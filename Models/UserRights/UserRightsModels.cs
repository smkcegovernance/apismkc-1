using System.Collections.Generic;

namespace SmkcApi.Models.UserRights
{
    public class UserRightsDto
    {
        public string UserId { get; set; }
        public bool IsAdmin { get; set; }
        public string DeptCode { get; set; }
        public string DeptName { get; set; }
        public bool HasCustomRights { get; set; }
        public List<DeptRightsItemDto> Rights { get; set; }

        public UserRightsDto()
        {
            Rights = new List<DeptRightsItemDto>();
        }
    }

    public class DeptRightsItemDto
    {
        public string DeptKey { get; set; }
        public List<string> MenuItems { get; set; }

        public DeptRightsItemDto()
        {
            MenuItems = new List<string>();
        }
    }

    public class SaveUserRightsRequest
    {
        /// <summary>Target user whose rights are being saved.</summary>
        public string UserId { get; set; }

        /// <summary>List of dept+menu assignments to persist.</summary>
        public List<DeptRightsItemDto> Rights { get; set; }

        /// <summary>Admin user performing the save (must be ADMIN001 or PTTEST01).</summary>
        public string AdminUserId { get; set; }
    }

    public class BulkSeedRequest
    {
        /// <summary>ERP dept key (e.g. "accounts").</summary>
        public string DeptKey { get; set; }

        /// <summary>Target user IDs to receive the rights.</summary>
        public List<string> UserIds { get; set; }

        /// <summary>Menu item keys to grant.</summary>
        public List<string> MenuItems { get; set; }

        /// <summary>Admin user performing the action.</summary>
        public string AdminUserId { get; set; }
    }

    public class ErpUserInfoDto
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string DeptCode { get; set; }
        public string DeptName { get; set; }
        public bool HasRights { get; set; }
    }
}
