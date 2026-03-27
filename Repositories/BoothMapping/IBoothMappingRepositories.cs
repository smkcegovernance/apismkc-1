using System.Collections.Generic;
using SmkcApi.Models.BoothMapping;

namespace SmkcApi.Repositories.BoothMapping
{
    /// <summary>
    /// Repository interface for booth mapping authentication operations
    /// Uses ULBERP schema for user authentication
    /// </summary>
    public interface IBoothAuthRepository
    {
        /// <summary>
        /// Authenticates a user using ULBERP schema
        /// </summary>
        /// <param name="userId">8-character user ID</param>
        /// <param name="password">User password</param>
        /// <returns>API response with login data</returns>
        BoothApiResponse<BoothLoginResponse> Login(string userId, string password);
    }

    /// <summary>
    /// Repository interface for booth data operations
    /// Uses WEBSITE schema for booth management
    /// </summary>
    public interface IBoothRepository
    {
        /// <summary>
        /// Gets statistics for booth mapping
        /// </summary>
        /// <param name="userId">Optional user ID to get user-specific stats</param>
        /// <returns>API response with statistics</returns>
        BoothApiResponse<BoothStatisticsResponse> GetStatistics(string userId = null);

        /// <summary>
        /// Gets all booths from the database
        /// </summary>
        /// <returns>API response with list of booths</returns>
        BoothApiResponse<List<BoothResponse>> GetAllBooths();

        /// <summary>
        /// Searches booths with optional filters
        /// </summary>
        /// <param name="boothNo">Booth number filter</param>
        /// <param name="boothName">Booth name filter (Hindi or English)</param>
        /// <param name="boothAddress">Address filter (Hindi or English)</param>
        /// <param name="wardNo">Ward number filter</param>
        /// <param name="isMapped">Mapping status filter (0=unmapped, 1=mapped)</param>
        /// <returns>API response with filtered booth list</returns>
        BoothApiResponse<List<BoothResponse>> SearchBooths(
            string boothNo, 
            string boothName, 
            string boothAddress, 
            string wardNo, 
            int? isMapped);

        /// <summary>
        /// Updates booth GPS location
        /// </summary>
        /// <param name="boothId">Booth ID</param>
        /// <param name="latitude">GPS latitude</param>
        /// <param name="longitude">GPS longitude</param>
        /// <param name="userId">User making the update</param>
        /// <param name="remarks">Optional remarks</param>
        /// <returns>API response with updated booth data</returns>
        BoothApiResponse<BoothResponse> UpdateBoothLocation(
            string boothId, 
            decimal latitude, 
            decimal longitude, 
            string userId, 
            string remarks);
    }
}
