using System.Collections.Generic;
using SmkcApi.Models.BoothMapping;
using SmkcApi.Repositories.BoothMapping;

namespace SmkcApi.Services.BoothMapping
{
    /// <summary>
    /// Service implementation for booth mapping authentication
    /// </summary>
    public class BoothAuthService : IBoothAuthService
    {
        private readonly IBoothAuthRepository _authRepository;

        public BoothAuthService(IBoothAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        public BoothApiResponse<BoothLoginResponse> Login(string userId, string password)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BoothApiResponse<BoothLoginResponse>.CreateError("User ID is required");
            }

            if (userId.Length != 8)
            {
                return BoothApiResponse<BoothLoginResponse>.CreateError("User ID must be exactly 8 characters");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return BoothApiResponse<BoothLoginResponse>.CreateError("Password is required");
            }

            return _authRepository.Login(userId, password);
        }
    }

    /// <summary>
    /// Service implementation for booth mapping operations
    /// </summary>
    public class BoothMappingService : IBoothMappingService
    {
        private readonly IBoothRepository _boothRepository;

        public BoothMappingService(IBoothRepository boothRepository)
        {
            _boothRepository = boothRepository;
        }

        public BoothApiResponse<BoothStatisticsResponse> GetStatistics(string userId = null)
        {
            // Optional: Validate userId format if provided
            if (!string.IsNullOrWhiteSpace(userId) && userId.Length != 8)
            {
                return BoothApiResponse<BoothStatisticsResponse>.CreateError("User ID must be exactly 8 characters");
            }

            return _boothRepository.GetStatistics(userId);
        }

        public BoothApiResponse<List<BoothResponse>> GetAllBooths()
        {
            return _boothRepository.GetAllBooths();
        }

        public BoothApiResponse<List<BoothResponse>> SearchBooths(
            string boothNo, 
            string boothName, 
            string boothAddress, 
            string wardNo, 
            int? isMapped)
        {
            // Validate isMapped value if provided
            if (isMapped.HasValue && (isMapped.Value < 0 || isMapped.Value > 1))
            {
                return BoothApiResponse<List<BoothResponse>>.CreateError("isMapped must be 0 (unmapped) or 1 (mapped)");
            }

            return _boothRepository.SearchBooths(boothNo, boothName, boothAddress, wardNo, isMapped);
        }

        public BoothApiResponse<BoothResponse> UpdateBoothLocation(
            string boothId, 
            decimal latitude, 
            decimal longitude, 
            string userId, 
            string remarks)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(boothId))
            {
                return BoothApiResponse<BoothResponse>.CreateError("Booth ID is required");
            }

            if (latitude < -90 || latitude > 90)
            {
                return BoothApiResponse<BoothResponse>.CreateError("Latitude must be between -90 and 90");
            }

            if (longitude < -180 || longitude > 180)
            {
                return BoothApiResponse<BoothResponse>.CreateError("Longitude must be between -180 and 180");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return BoothApiResponse<BoothResponse>.CreateError("User ID is required");
            }

            if (userId.Length != 8)
            {
                return BoothApiResponse<BoothResponse>.CreateError("User ID must be exactly 8 characters");
            }

            return _boothRepository.UpdateBoothLocation(boothId, latitude, longitude, userId, remarks);
        }
    }
}
