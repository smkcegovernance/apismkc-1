using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Models.Departments;

namespace SmkcApi.Repositories.Departments
{
    public class DepartmentsRepository : IDepartmentsRepository
    {
        private readonly IOracleConnectionFactory _connFactory;

        public DepartmentsRepository(IOracleConnectionFactory connFactory)
        {
            _connFactory = connFactory ?? throw new ArgumentNullException("connFactory");
        }

        public IEnumerable<DeptConfigDto> GetActiveDepts(int ulbCode)
        {
            // Join ERP_DEPT_CONFIG with DEPARTMENTDET to get bilingual names.
            // DEPT_NAMELL_UNICODE is NVARCHAR2 — read via GetValue() to avoid ODP.NET quirks.
            const string sql = @"
                SELECT c.DEPT_CODE,
                       c.ROUTE_KEY,
                       NVL(d.DEPT_NAME, c.ROUTE_KEY)               AS NAME_EN,
                       NVL(d.DEPT_NAMELL_UNICODE, d.DEPT_NAME)     AS NAME_MR,
                       c.ICON_CLASS,
                       c.ACCENT_COLOR,
                       c.COLOR_BG,
                       c.DISPLAY_ORDER
                  FROM ULBERP.ERP_DEPT_CONFIG c
                  LEFT JOIN ULBERP.DEPARTMENTDET d
                    ON d.DEPT_CODE = c.DEPT_CODE
                   AND d.ULB_CODE  = :ulb_code
                 WHERE c.IS_ACTIVE = 1
                 ORDER BY c.DISPLAY_ORDER, c.DEPT_CODE";

            var list = new List<DeptConfigDto>();

            using (var conn = _connFactory.CreateUlberp())
            using (var cmd  = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;

                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new DeptConfigDto
                        {
                            DeptCode     = Convert.ToInt32(rdr.GetValue(0)),
                            RouteKey     = rdr.IsDBNull(1) ? null : rdr.GetString(1),
                            NameEn       = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                            NameMr       = rdr.IsDBNull(3) ? null : rdr.GetValue(3)?.ToString(),
                            Icon         = rdr.IsDBNull(4) ? "bi-building"           : rdr.GetString(4),
                            Color        = rdr.IsDBNull(5) ? "#1565C0"               : rdr.GetString(5),
                            ColorBg      = rdr.IsDBNull(6) ? "rgba(21,101,192,0.08)" : rdr.GetString(6),
                            DisplayOrder = rdr.IsDBNull(7) ? 99 : Convert.ToInt32(rdr.GetValue(7)),
                        });
                    }
                }
            }

            return list;
        }
    }
}
