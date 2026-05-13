using System.Collections.Generic;
using SmkcApi.Models.Departments;

namespace SmkcApi.Repositories.Departments
{
    public interface IDepartmentsRepository
    {
        IEnumerable<DeptConfigDto> GetActiveDepts(int ulbCode);
    }
}
