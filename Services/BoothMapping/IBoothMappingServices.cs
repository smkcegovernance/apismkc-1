using System.Collections.Generic;
using SmkcApi.Models.BoothMapping;

namespace SmkcApi.Services.BoothMapping
{
    /// <summary>
    /// Service interface for booth mapping authentication
    /// </summary>
    public interface IBoothAuthService
    {
        /// <summary>
        /// Authenticates a user for booth mapping application
        /// </summary>
        BoothApiResponse<BoothLoginResponse> Login(string userId, string password);
    }

    /// <summary>
    /// Service interface for booth mapping operations
    /// </summary>
    public interface IBoothMappingService
    {
        /// <summary>
        /// Gets booth mapping statistics
        /// </summary>
        BoothApiResponse<BoothStatisticsResponse> GetStatistics(string userId = null);

        /// <summary>
        /// Gets all booths
        /// </summary>
        BoothApiResponse<List<BoothResponse>> GetAllBooths();

        /// <summary>
        /// Searches booths with filters
        /// </summary>
        BoothApiResponse<List<BoothResponse>> SearchBooths(
            string boothNo, 
            string boothName, 
            string boothAddress, 
            string wardNo, 
            int? isMapped);

        /// <summary>
        /// Updates booth GPS location
        /// </summary>
        BoothApiResponse<BoothResponse> UpdateBoothLocation(
            string boothId, 
            decimal latitude, 
            decimal longitude, 
            string userId, 
            string remarks);
    }
}
