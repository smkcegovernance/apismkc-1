using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Models.UserRights;

namespace SmkcApi.Repositories.UserRights
{
    public class UserRightsRepository : IUserRightsRepository
    {
        private static readonly HashSet<string> AdminUserIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ADMIN001", "PTTEST01" };

        // Dept codes: 524 = Accounts, 526 = Audit
        private const string AccountsDeptCode = "524";
        private const string AuditDeptCode    = "526";

        private static readonly string[] AccountsMenus = {
            "primary-budget-entry", "final-budget-entry", "budget-book-list",
            "budget-cap",           "work-proposal-remarks", "deposit-manager"
        };
        private static readonly string[] AuditMenus = {
            "work-proposal-remarks"
        };

        // Complete menu catalogue — mirrors dept-menus.ts (used for admin seeding)
        private static readonly Tuple<string, string>[] AllMenuGrants = new[] {
            Tuple.Create("general-administration", "work-proposal-under-10l"),
            Tuple.Create("general-administration", "work-proposal-over-10l"),
            Tuple.Create("general-administration", "work-proposal-other"),
            Tuple.Create("general-administration", "work-proposals"),
            Tuple.Create("accounts",               "primary-budget-entry"),
            Tuple.Create("accounts",               "final-budget-entry"),
            Tuple.Create("accounts",               "budget-book-list"),
            Tuple.Create("accounts",               "budget-cap"),
            Tuple.Create("accounts",               "work-proposal-remarks"),
            Tuple.Create("accounts",               "deposit-manager"),
            Tuple.Create("audit-department",        "work-proposal-remarks"),
            Tuple.Create("women-child-welfare",     "disability-registration"),
            Tuple.Create("women-child-welfare",     "registrations"),
        };

        private readonly IOracleConnectionFactory _connFactory;

        public UserRightsRepository(IOracleConnectionFactory connFactory)
        {
            _connFactory = connFactory ?? throw new ArgumentNullException("connFactory");
        }

        // ── GetUserRights ────────────────────────────────────────────────────────

        public UserRightsDto GetUserRights(int ulbCode, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", "userId");

            var uid = userId.Trim().ToUpper();
            bool isAdmin = AdminUserIds.Contains(uid);

            string deptCode = null;
            string deptName = null;

            // Fetch dept info from USERDET + DEPARTMENTDET
            const string userSql = @"
                SELECT u.DEPT_CODE,
                       NVL(d.DEPT_NAMELL_UNICODE, d.DEPT_NAME) AS DEPT_NAME
                  FROM ULBERP.USERDET u
                  LEFT JOIN ULBERP.DEPARTMENTDET d
                    ON d.DEPT_CODE = u.DEPT_CODE AND d.ULB_CODE = u.ULB_CODE
                 WHERE u.USER_ID = :user_id
                   AND u.ULB_CODE = :ulb_code";

            // Fetch custom rights rows
            const string rightsSql = @"
                SELECT DEPT_KEY, MENU_ITEM
                  FROM ULBERP.ERP_USER_RIGHTS
                 WHERE USER_ID  = :user_id
                   AND ULB_CODE = :ulb_code
                 ORDER BY DEPT_KEY, MENU_ITEM";

            var grouped = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            using (var conn = _connFactory.CreateUlberp())
            {
                conn.Open();

                // User dept info
                using (var cmd = new OracleCommand(userSql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("user_id",  OracleDbType.Varchar2).Value = uid;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value  = ulbCode;

                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            deptCode = rdr.IsDBNull(0) ? null : rdr.GetValue(0).ToString();
                            deptName = rdr.IsDBNull(1) ? null : rdr.GetString(1);
                        }
                    }
                }

                // Rights rows
                using (var cmd = new OracleCommand(rightsSql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("user_id",  OracleDbType.Varchar2).Value = uid;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value  = ulbCode;

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var dKey  = rdr.IsDBNull(0) ? null : rdr.GetString(0);
                            var mItem = rdr.IsDBNull(1) ? null : rdr.GetString(1);
                            if (dKey == null || mItem == null) continue;

                            if (!grouped.ContainsKey(dKey))
                                grouped[dKey] = new List<string>();
                            grouped[dKey].Add(mItem);
                        }
                    }
                }
            }

            // Auto-grant based on dept code (overrides need for manual seeding)
            if (deptCode == AccountsDeptCode)
            {
                if (!grouped.ContainsKey("accounts")) grouped["accounts"] = new List<string>();
                foreach (var m in AccountsMenus)
                    if (!grouped["accounts"].Contains(m, StringComparer.OrdinalIgnoreCase))
                        grouped["accounts"].Add(m);
            }
            if (deptCode == AuditDeptCode)
            {
                if (!grouped.ContainsKey("audit-department")) grouped["audit-department"] = new List<string>();
                foreach (var m in AuditMenus)
                    if (!grouped["audit-department"].Contains(m, StringComparer.OrdinalIgnoreCase))
                        grouped["audit-department"].Add(m);
            }

            var rights = grouped.Select(kv => new DeptRightsItemDto
            {
                DeptKey   = kv.Key,
                MenuItems = kv.Value,
            }).ToList();

            return new UserRightsDto
            {
                UserId          = uid,
                IsAdmin         = isAdmin,
                HasCustomRights = rights.Count > 0,
                DeptCode        = deptCode,
                DeptName        = deptName,
                Rights          = rights,
            };
        }

        // ── GetAllUsers ──────────────────────────────────────────────────────────

        public IEnumerable<ErpUserInfoDto> GetAllUsers(int ulbCode)
        {
            const string sql = @"
                SELECT u.USER_ID,
                       NVL(u.USER_NAME, u.USER_ID)               AS EMP_NAME,
                       TO_CHAR(u.DEPT_CODE)                      AS DEPT_CODE,
                       NVL(d.DEPT_NAMELL_UNICODE, d.DEPT_NAME)   AS DEPT_NAME,
                       CASE WHEN r.USER_ID IS NOT NULL THEN 1 ELSE 0 END AS HAS_RIGHTS
                  FROM ULBERP.USERDET u
                  LEFT JOIN ULBERP.DEPARTMENTDET d
                    ON d.DEPT_CODE = u.DEPT_CODE AND d.ULB_CODE = u.ULB_CODE
                  LEFT JOIN (
                    SELECT DISTINCT USER_ID
                      FROM ULBERP.ERP_USER_RIGHTS
                     WHERE ULB_CODE = :ulb
                  ) r ON r.USER_ID = u.USER_ID
                 WHERE u.ULB_CODE = :ulb
                   AND u.USER_VIFLAG = 'V'
                 ORDER BY u.USER_ID";

            var list = new List<ErpUserInfoDto>();

            using (var conn = _connFactory.CreateUlberp())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ulb", OracleDbType.Decimal).Value = ulbCode;

                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new ErpUserInfoDto
                        {
                            UserId    = rdr.IsDBNull(0) ? null : rdr.GetString(0),
                            Name      = rdr.IsDBNull(1) ? null : rdr.GetString(1),
                            DeptCode  = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                            DeptName  = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                            HasRights = !rdr.IsDBNull(4) && Convert.ToInt32(rdr.GetValue(4)) == 1,
                        });
                    }
                }
            }

            return list;
        }

        // ── SaveUserRights ───────────────────────────────────────────────────────

        public void SaveUserRights(int ulbCode, string userId,
            IEnumerable<DeptRightsItemDto> rights, string adminUserId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", "userId");

            var uid    = userId.Trim().ToUpper();
            var grantBy = (adminUserId ?? "SYS").Trim().ToUpper();

            const string deleteSql = @"
                DELETE FROM ULBERP.ERP_USER_RIGHTS
                 WHERE USER_ID  = :user_id
                   AND ULB_CODE = :ulb_code";

            const string insertSql = @"
                INSERT INTO ULBERP.ERP_USER_RIGHTS
                       (ULB_CODE, USER_ID, DEPT_KEY, MENU_ITEM, GRANTED_BY, GRANTED_DT)
                VALUES (:ulb_code, :user_id, :dkey, :mitem, :gby, SYSDATE)";

            using (var conn = _connFactory.CreateUlberp())
            {
                conn.Open();
                using (var txn = conn.BeginTransaction())
                {
                    try
                    {
                        // Delete existing rights
                        using (var del = new OracleCommand(deleteSql, conn))
                        {
                            del.Transaction = txn;
                            del.BindByName  = true;
                            del.Parameters.Add("user_id",  OracleDbType.Varchar2).Value = uid;
                            del.Parameters.Add("ulb_code", OracleDbType.Decimal).Value  = ulbCode;
                            del.ExecuteNonQuery();
                        }

                        // Insert new rights
                        var rightsArr = (rights ?? Enumerable.Empty<DeptRightsItemDto>()).ToList();
                        foreach (var dept in rightsArr)
                        {
                            if (string.IsNullOrWhiteSpace(dept.DeptKey)) continue;
                            foreach (var item in (dept.MenuItems ?? Enumerable.Empty<string>()))
                            {
                                if (string.IsNullOrWhiteSpace(item)) continue;
                                using (var ins = new OracleCommand(insertSql, conn))
                                {
                                    ins.Transaction = txn;
                                    ins.BindByName  = true;
                                    ins.Parameters.Add("ulb_code", OracleDbType.Decimal).Value  = ulbCode;
                                    ins.Parameters.Add("user_id",  OracleDbType.Varchar2).Value = uid;
                                    ins.Parameters.Add("dkey",  OracleDbType.Varchar2).Value = dept.DeptKey.Trim();
                                    ins.Parameters.Add("mitem", OracleDbType.Varchar2).Value = item.Trim();
                                    ins.Parameters.Add("gby",   OracleDbType.Varchar2).Value = grantBy;
                                    ins.ExecuteNonQuery();
                                }
                            }
                        }

                        txn.Commit();
                    }
                    catch
                    {
                        txn.Rollback();
                        throw;
                    }
                }
            }
        }

        // ── BulkSeedRights ───────────────────────────────────────────────────────

        public void BulkSeedRights(int ulbCode, string deptKey,
            IEnumerable<string> userIds, IEnumerable<string> menuItems,
            string adminUserId)
        {
            if (string.IsNullOrWhiteSpace(deptKey))
                throw new ArgumentException("deptKey is required", "deptKey");

            var grantBy   = (adminUserId ?? "SYS").Trim().ToUpper();
            var dKey      = deptKey.Trim();
            var userList  = (userIds  ?? Enumerable.Empty<string>()).Select(u => u.Trim().ToUpper()).Where(u => u.Length > 0).ToList();
            var itemList  = (menuItems ?? Enumerable.Empty<string>()).Select(m => m.Trim()).Where(m => m.Length > 0).ToList();

            if (!userList.Any() || !itemList.Any()) return;

            const string insertSql = @"
                MERGE INTO ULBERP.ERP_USER_RIGHTS tgt
                USING (SELECT :ulb AS C1, :uid AS C2, :dkey AS C3, :mitem AS C4 FROM DUAL) src
                   ON (tgt.ULB_CODE = src.C1 AND tgt.USER_ID = src.C2
                       AND tgt.DEPT_KEY = src.C3 AND tgt.MENU_ITEM = src.C4)
                WHEN NOT MATCHED THEN
                  INSERT (ULB_CODE, USER_ID, DEPT_KEY, MENU_ITEM, GRANTED_BY, GRANTED_DT)
                  VALUES (src.C1, src.C2, src.C3, src.C4, :gby, SYSDATE)";

            using (var conn = _connFactory.CreateUlberp())
            {
                conn.Open();
                using (var txn = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var uid in userList)
                        {
                            foreach (var mitem in itemList)
                            {
                                using (var cmd = new OracleCommand(insertSql, conn))
                                {
                                    cmd.Transaction = txn;
                                    cmd.BindByName  = true;
                                    cmd.Parameters.Add("ulb",    OracleDbType.Decimal).Value  = ulbCode;
                                    cmd.Parameters.Add("uid",    OracleDbType.Varchar2).Value = uid;
                                    cmd.Parameters.Add("dkey",   OracleDbType.Varchar2).Value = dKey;
                                    cmd.Parameters.Add("mitem",  OracleDbType.Varchar2).Value = mitem;
                                    cmd.Parameters.Add("gby",    OracleDbType.Varchar2).Value = grantBy;
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        txn.Commit();
                    }
                    catch
                    {
                        txn.Rollback();
                        throw;
                    }
                }
            }
        }

        // ── ClearUserRights ──────────────────────────────────────────────────────

        public void ClearUserRights(int ulbCode, string userId, string adminUserId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", "userId");

            var uid = userId.Trim().ToUpper();

            const string sql = @"
                DELETE FROM ULBERP.ERP_USER_RIGHTS
                 WHERE USER_ID  = :user_id
                   AND ULB_CODE = :ulb_code";

            using (var conn = _connFactory.CreateUlberp())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("user_id",  OracleDbType.Varchar2).Value = uid;
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value  = ulbCode;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ── SeedDefaultRights ────────────────────────────────────────────────────

        public string SeedDefaultRights(int ulbCode, string adminUserId)
        {
            var grantBy = (adminUserId ?? "SYS").Trim().ToUpper();

            // Known menu keys for each dept (mirrors dept-menus.ts)
            var gadMenus = new[] {
                "work-proposal-under-10l", "work-proposal-over-10l",
                "work-proposal-other",     "work-proposals"
            };
            // Reuse static arrays (dept codes 524/526)
            var accountsMenus = AccountsMenus;
            var auditMenus    = AuditMenus;

            // Fetch all active users with their dept code
            const string userSql = @"
                SELECT u.USER_ID,
                       TO_CHAR(u.DEPT_CODE) AS DEPT_CODE_NUM
                  FROM ULBERP.USERDET u
                 WHERE u.ULB_CODE = :ulb
                   AND u.USER_VIFLAG = 'V'";

            // INSERT with duplicate guard using MERGE (avoids ORA-01745 with positional bind vars)
            const string mergeSql = @"
                MERGE INTO ULBERP.ERP_USER_RIGHTS tgt
                USING (SELECT :ulb AS C1, :uid AS C2, :dkey AS C3, :mitem AS C4 FROM DUAL) src
                   ON (tgt.ULB_CODE = src.C1 AND tgt.USER_ID = src.C2
                       AND tgt.DEPT_KEY = src.C3 AND tgt.MENU_ITEM = src.C4)
                WHEN NOT MATCHED THEN
                  INSERT (ULB_CODE, USER_ID, DEPT_KEY, MENU_ITEM, GRANTED_BY, GRANTED_DT)
                  VALUES (src.C1, src.C2, src.C3, src.C4, :gby, SYSDATE)";

            int inserted = 0;
            int users    = 0;

            using (var conn = _connFactory.CreateUlberp())
            {
                conn.Open();

                // Load users
                var userRows = new List<Tuple<string, string>>();
                using (var cmd = new OracleCommand(userSql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ulb", OracleDbType.Decimal).Value = ulbCode;
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var uid     = rdr.IsDBNull(0) ? null : rdr.GetString(0);
                            var deptNum = rdr.IsDBNull(1) ? ""   : rdr.GetString(1);
                            if (uid != null) userRows.Add(Tuple.Create(uid, deptNum));
                        }
                    }
                }

                users = userRows.Count;

                using (var txn = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var row in userRows)
                        {
                            var uid     = row.Item1;
                            var deptNum = row.Item2;

                            // Determine which extra menus this user gets (by dept code)
                            bool isAccount = deptNum == AccountsDeptCode; // 524
                            bool isAudit   = deptNum == AuditDeptCode;    // 526

                            // Build the full set of (deptKey, menuItem) pairs
                            var grants = new List<Tuple<string, string>>();
                            foreach (var m in gadMenus)
                                grants.Add(Tuple.Create("general-administration", m));
                            if (isAccount)
                                foreach (var m in accountsMenus)
                                    grants.Add(Tuple.Create("accounts", m));
                            if (isAudit)
                                foreach (var m in auditMenus)
                                    grants.Add(Tuple.Create("audit-department", m));

                            foreach (var g in grants)
                            {
                                using (var cmd = new OracleCommand(mergeSql, conn))
                                {
                                    cmd.Transaction = txn;
                                    cmd.BindByName  = true;
                                    cmd.Parameters.Add("ulb",   OracleDbType.Decimal).Value  = ulbCode;
                                    cmd.Parameters.Add("uid",   OracleDbType.Varchar2).Value = uid;
                                    cmd.Parameters.Add("dkey",  OracleDbType.Varchar2).Value = g.Item1;
                                    cmd.Parameters.Add("mitem", OracleDbType.Varchar2).Value = g.Item2;
                                    cmd.Parameters.Add("gby",   OracleDbType.Varchar2).Value = grantBy;
                                    inserted += cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Seed admin users with ALL menus from every department
                        foreach (var adminUid in AdminUserIds)
                        {
                            foreach (var g in AllMenuGrants)
                            {
                                using (var cmd = new OracleCommand(mergeSql, conn))
                                {
                                    cmd.Transaction = txn;
                                    cmd.BindByName  = true;
                                    cmd.Parameters.Add("ulb",   OracleDbType.Decimal).Value  = ulbCode;
                                    cmd.Parameters.Add("uid",   OracleDbType.Varchar2).Value = adminUid.ToUpper();
                                    cmd.Parameters.Add("dkey",  OracleDbType.Varchar2).Value = g.Item1;
                                    cmd.Parameters.Add("mitem", OracleDbType.Varchar2).Value = g.Item2;
                                    cmd.Parameters.Add("gby",   OracleDbType.Varchar2).Value = grantBy;
                                    inserted += cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        txn.Commit();
                    }
                    catch
                    {
                        txn.Rollback();
                        throw;
                    }
                }
            }

            return string.Format(
                "Seeded defaults for {0} users — {1} new rights inserted. " +
                "GAD access granted to all; Accounts (dept 524) and Audit (dept 526) by dept code.",
                users, inserted);
        }
    }
}
