using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SmkcApi.Models;
using SmkcApi.Models.VotingStatistics;
using SmkcApi.Repositories.VotingStatistics;

namespace SmkcApi.Controllers.VotingStatistics
{
    /// <summary>
    /// Voting Statistics API Controller for SMKC Election 2025
    /// Provides endpoints to retrieve and update voting statistics
    /// Secured with SHA-256 HMAC authentication
    /// </summary>
    [RoutePrefix("api/voting-statistics")]
    public class VotingStatisticsController : ApiController
    {
        private readonly IVotingStatisticsRepository _repository;

        public VotingStatisticsController(IVotingStatisticsRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// GET /api/voting-statistics
        /// Retrieves the latest active voting statistics record
        /// Requires SHA-256 authentication
        /// </summary>
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetVotingStatistics()
        {
            try
            {
                var response = _repository.GetVotingStatistics();

                if (!response.Success)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        status = "error",
                        message = response.Message ?? "No active voting statistics found",
                        code = "NOT_FOUND"
                    });
                }

                var dataTable = response.Data as DataTable;
                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        status = "error",
                        message = "No active voting statistics found",
                        code = "NOT_FOUND"
                    });
                }

                var row = dataTable.Rows[0];
                var data = MapVotingStatisticsData(row);

                return Ok(new
                {
                    status = "success",
                    data = new
                    {
                        id = data.Id,
                        totalVoters = data.TotalVoters,
                        maleVoters = data.MaleVoters,
                        femaleVoters = data.FemaleVoters,
                        otherVoters = data.OtherVoters,
                        castedVotes = data.CastedVotes,
                        maleCasted = data.MaleCasted,
                        femaleCasted = data.FemaleCasted,
                        otherCasted = data.OtherCasted,
                        timeSlot = data.TimeSlot,
                        overallTurnoutPercent = data.OverallTurnoutPercent,
                        maleTurnoutPercent = data.MaleTurnoutPercent,
                        femaleTurnoutPercent = data.FemaleTurnoutPercent,
                        otherTurnoutPercent = data.OtherTurnoutPercent,
                        createdDate = data.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        updatedDate = data.UpdatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        createdBy = data.CreatedBy,
                        updatedBy = data.UpdatedBy,
                        isActive = data.IsActive,
                        remarks = data.Remarks
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in GetVotingStatistics: {ex.Message}\n{ex.StackTrace}");
                return Content(HttpStatusCode.InternalServerError, new
                {
                    status = "error",
                    message = "Internal server error",
                    code = "SERVER_ERROR"
                });
            }
        }

        /// <summary>
        /// GET /api/voting-statistics/latest
        /// Returns formatted statistics with calculated percentages
        /// Requires SHA-256 authentication
        /// </summary>
        [HttpGet]
        [Route("latest")]
        public IHttpActionResult GetLatestStatistics()
        {
            try
            {
                var response = _repository.GetLatestStatistics();

                if (!response.Success)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        status = "error",
                        message = response.Message ?? "No active voting statistics found",
                        code = "NOT_FOUND"
                    });
                }

                var dataTable = response.Data as DataTable;
                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        status = "error",
                        message = "No active voting statistics found",
                        code = "NOT_FOUND"
                    });
                }

                var row = dataTable.Rows[0];

                var formattedResponse = new
                {
                    status = "success",
                    data = new
                    {
                        statistics = new
                        {
                            totalVoters = Convert.ToInt32(row["TOTAL_VOTERS"]),
                            castedVotes = Convert.ToInt32(row["CASTED_VOTES"]),
                            turnoutPercentage = row["OVERALL_TURNOUT_PERCENT"] != DBNull.Value 
                                ? Convert.ToDecimal(row["OVERALL_TURNOUT_PERCENT"]) : 0
                        },
                        breakdown = new
                        {
                            male = new
                            {
                                totalVoters = Convert.ToInt32(row["MALE_VOTERS"]),
                                castedVotes = Convert.ToInt32(row["MALE_CASTED"]),
                                turnoutPercentage = row["MALE_TURNOUT_PERCENT"] != DBNull.Value 
                                    ? Convert.ToDecimal(row["MALE_TURNOUT_PERCENT"]) : 0
                            },
                            female = new
                            {
                                totalVoters = Convert.ToInt32(row["FEMALE_VOTERS"]),
                                castedVotes = Convert.ToInt32(row["FEMALE_CASTED"]),
                                turnoutPercentage = row["FEMALE_TURNOUT_PERCENT"] != DBNull.Value 
                                    ? Convert.ToDecimal(row["FEMALE_TURNOUT_PERCENT"]) : 0
                            },
                            other = new
                            {
                                totalVoters = Convert.ToInt32(row["OTHER_VOTERS"]),
                                castedVotes = Convert.ToInt32(row["OTHER_CASTED"]),
                                turnoutPercentage = row["OTHER_TURNOUT_PERCENT"] != DBNull.Value 
                                    ? Convert.ToDecimal(row["OTHER_TURNOUT_PERCENT"]) : 0
                            }
                        },
                        timeSlot = row["TIME_SLOT"]?.ToString(),
                        lastUpdated = Convert.ToDateTime(row["UPDATED_DATE"]).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        updatedBy = row["UPDATED_BY"]?.ToString()
                    }
                };

                return Ok(formattedResponse);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in GetLatestStatistics: {ex.Message}\n{ex.StackTrace}");
                return Content(HttpStatusCode.InternalServerError, new
                {
                    status = "error",
                    message = "Internal server error",
                    code = "SERVER_ERROR"
                });
            }
        }

        /// <summary>
        /// POST /api/voting-statistics/update
        /// Updates or creates voting statistics
        /// Requires SHA-256 authentication
        /// </summary>
        [HttpPost]
        [Route("update")]
        public IHttpActionResult UpdateVotingStatistics([FromBody] UpdateVotingStatisticsRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        status = "error",
                        message = "Request body is required",
                        code = "VALIDATION_ERROR"
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Content(HttpStatusCode.BadRequest, new
                    {
                        status = "error",
                        message = "Validation failed",
                        errors = errors,
                        code = "VALIDATION_ERROR"
                    });
                }

                // Additional business validation
                var validationErrors = ValidateBusinessRules(request);
                if (validationErrors.Any())
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        status = "error",
                        message = "Validation failed",
                        errors = validationErrors,
                        code = "VALIDATION_ERROR"
                    });
                }

                var response = _repository.UpdateVotingStatistics(request);

                if (!response.Success)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        status = "error",
                        message = response.Message ?? "Failed to update voting statistics",
                        data = response.Data
                    });
                }

                return Ok(new
                {
                    status = "success",
                    message = response.Message,
                    data = response.Data
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in UpdateVotingStatistics: {ex.Message}\n{ex.StackTrace}");
                return Content(HttpStatusCode.InternalServerError, new
                {
                    status = "error",
                    message = "Internal server error",
                    code = "SERVER_ERROR"
                });
            }
        }

        #region Helper Methods

        private VotingStatisticsData MapVotingStatisticsData(DataRow row)
        {
            return new VotingStatisticsData
            {
                Id = Convert.ToInt32(row["ID"]),
                TotalVoters = Convert.ToInt32(row["TOTAL_VOTERS"]),
                MaleVoters = Convert.ToInt32(row["MALE_VOTERS"]),
                FemaleVoters = Convert.ToInt32(row["FEMALE_VOTERS"]),
                OtherVoters = Convert.ToInt32(row["OTHER_VOTERS"]),
                CastedVotes = Convert.ToInt32(row["CASTED_VOTES"]),
                MaleCasted = Convert.ToInt32(row["MALE_CASTED"]),
                FemaleCasted = Convert.ToInt32(row["FEMALE_CASTED"]),
                OtherCasted = Convert.ToInt32(row["OTHER_CASTED"]),
                TimeSlot = row["TIME_SLOT"]?.ToString(),
                OverallTurnoutPercent = row["OVERALL_TURNOUT_PERCENT"] != DBNull.Value 
                    ? Convert.ToDecimal(row["OVERALL_TURNOUT_PERCENT"]) : 0,
                MaleTurnoutPercent = row["MALE_TURNOUT_PERCENT"] != DBNull.Value 
                    ? Convert.ToDecimal(row["MALE_TURNOUT_PERCENT"]) : 0,
                FemaleTurnoutPercent = row["FEMALE_TURNOUT_PERCENT"] != DBNull.Value 
                    ? Convert.ToDecimal(row["FEMALE_TURNOUT_PERCENT"]) : 0,
                OtherTurnoutPercent = row["OTHER_TURNOUT_PERCENT"] != DBNull.Value 
                    ? Convert.ToDecimal(row["OTHER_TURNOUT_PERCENT"]) : 0,
                CreatedDate = Convert.ToDateTime(row["CREATED_DATE"]),
                UpdatedDate = Convert.ToDateTime(row["UPDATED_DATE"]),
                CreatedBy = row["CREATED_BY"]?.ToString(),
                UpdatedBy = row["UPDATED_BY"]?.ToString(),
                IsActive = row["IS_ACTIVE"]?.ToString(),
                Remarks = row["REMARKS"] != DBNull.Value ? row["REMARKS"].ToString() : null
            };
        }

        private List<string> ValidateBusinessRules(UpdateVotingStatisticsRequest request)
        {
            var errors = new List<string>();

            if (request.CastedVotes > request.TotalVoters)
            {
                errors.Add("Casted votes cannot exceed total voters");
            }

            if (request.MaleCasted > request.MaleVoters)
            {
                errors.Add("Male casted votes cannot exceed male voters");
            }

            if (request.FemaleCasted > request.FemaleVoters)
            {
                errors.Add("Female casted votes cannot exceed female voters");
            }

            if (request.OtherCasted > request.OtherVoters)
            {
                errors.Add("Other casted votes cannot exceed other voters");
            }

            // Validate that sum of gender voters equals total voters (with tolerance for rounding)
            var sumGenderVoters = request.MaleVoters + request.FemaleVoters + request.OtherVoters;
            if (Math.Abs(sumGenderVoters - request.TotalVoters) > 1)
            {
                errors.Add("Sum of male, female, and other voters should equal total voters");
            }

            // Validate that sum of gender casted equals total casted (with tolerance for rounding)
            var sumGenderCasted = request.MaleCasted + request.FemaleCasted + request.OtherCasted;
            if (Math.Abs(sumGenderCasted - request.CastedVotes) > 1)
            {
                errors.Add("Sum of male, female, and other casted votes should equal total casted votes");
            }

            return errors;
        }

        #endregion
    }
}
