using System.Collections.Generic;
using System.Threading.Tasks;
using SmkcApi.Models;
using SmkcApi.Repositories;

namespace SmkcApi.Services
{
    public interface IWaterDashboardService
    {
        Task<WaterRevenueDashboard>    GetRevenueDashboardAsync(string finYr, string wardCode, string divCode);
        Task<WaterConnectionDashboard> GetConnectionDashboardAsync(string wardCode, string divCode);
        Task<List<DivisionItem>>       GetDivisionsAsync(string wardCode);
    }

    public class WaterDashboardService : IWaterDashboardService
    {
        private readonly IWaterDashboardRepository _repo;

        public WaterDashboardService(IWaterDashboardRepository repo)
        {
            _repo = repo;
        }

        public Task<WaterRevenueDashboard> GetRevenueDashboardAsync(string finYr, string wardCode, string divCode)
        {
            return _repo.GetRevenueDashboardAsync(finYr, wardCode, divCode);
        }

        public Task<WaterConnectionDashboard> GetConnectionDashboardAsync(string wardCode, string divCode)
        {
            return _repo.GetConnectionDashboardAsync(wardCode, divCode);
        }

        public Task<List<DivisionItem>> GetDivisionsAsync(string wardCode)
        {
            return _repo.GetDivisionsAsync(wardCode);
        }
    }
}
