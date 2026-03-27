using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmkcApi.Models.BoothMapping
{
    #region Request Models

    /// <summary>
    /// User login request for booth mapping application
    /// Uses ULBERP schema for authentication
    /// </summary>
    public class BoothLoginRequest
    {
        public string UserId { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Search/filter request for booth listing
    /// </summary>
    public class SearchBoothsRequest
    {
        public string BoothNo { get; set; }
        public string BoothName { get; set; }
        public string BoothAddress { get; set; }
        public string WardNo { get; set; }
        public int? IsMapped { get; set; }  // 0 = unmapped, 1 = mapped, null = all
    }

    /// <summary>
    /// Update booth GPS location request
    /// </summary>
    public class UpdateBoothLocationRequest
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string UserId { get; set; }
        public string Remarks { get; set; }
    }

    #endregion

    #region Response Models

    /// <summary>
    /// Login response data
    /// </summary>
    public class BoothLoginResponse
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
    }

    /// <summary>
    /// Booth statistics response
    /// </summary>
    public class BoothStatisticsResponse
    {
        public int TotalBooths { get; set; }
        public int MappedBooths { get; set; }
        public int UnmappedBooths { get; set; }
        public int UserMappedBooths { get; set; }
        public int UserUnmappedBooths { get; set; }
    }

    /// <summary>
    /// Booth details response
    /// </summary>
    public class BoothResponse
    {
        public string Id { get; set; }
        public string BoothNo { get; set; }
        public string BoothName { get; set; }
        public string BoothNameEnglish { get; set; }
        public string BoothAddress { get; set; }
        public string BoothAddressEnglish { get; set; }
        public string WardNo { get; set; }
        public string WardName { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string IsMapped { get; set; }  // "true" or "false" as string for consistency
        public string MappedBy { get; set; }
        public string MappedDate { get; set; }  // ISO 8601 format string
    }

    /// <summary>
    /// Generic API response wrapper
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    public class BoothApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public DateTime Timestamp { get; set; }

        public BoothApiResponse()
        {
            Timestamp = DateTime.UtcNow;
        }

        public static BoothApiResponse<T> CreateSuccess(T data, string message = "Operation successful")
        {
            return new BoothApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static BoothApiResponse<T> CreateError(string message, T data = default(T))
        {
            return new BoothApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = data
            };
        }
    }

    #endregion
}
