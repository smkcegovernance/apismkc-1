using System;

namespace SmkcApi.Models.VotingStatistics
{
    /// <summary>
    /// Request model for updating voting statistics
    /// Validation is performed in the controller
    /// </summary>
    public class UpdateVotingStatisticsRequest
    {
        public int TotalVoters { get; set; }
        public int MaleVoters { get; set; }
        public int FemaleVoters { get; set; }
        public int OtherVoters { get; set; }
        public int CastedVotes { get; set; }
        public int MaleCasted { get; set; }
        public int FemaleCasted { get; set; }
        public int OtherCasted { get; set; }
        public string TimeSlot { get; set; }
        public string UpdatedBy { get; set; }
    }

    /// <summary>
    /// Complete voting statistics data model
    /// </summary>
    public class VotingStatisticsData
    {
        public int Id { get; set; }
        public int TotalVoters { get; set; }
        public int MaleVoters { get; set; }
        public int FemaleVoters { get; set; }
        public int OtherVoters { get; set; }
        public int CastedVotes { get; set; }
        public int MaleCasted { get; set; }
        public int FemaleCasted { get; set; }
        public int OtherCasted { get; set; }
        public string TimeSlot { get; set; }
        public decimal OverallTurnoutPercent { get; set; }
        public decimal MaleTurnoutPercent { get; set; }
        public decimal FemaleTurnoutPercent { get; set; }
        public decimal OtherTurnoutPercent { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public string IsActive { get; set; }
        public string Remarks { get; set; }
    }

    /// <summary>
    /// Formatted statistics response for /latest endpoint
    /// </summary>
    public class FormattedStatisticsResponse
    {
        public StatisticsSummary Statistics { get; set; }
        public StatisticsBreakdown Breakdown { get; set; }
        public string TimeSlot { get; set; }
        public DateTime LastUpdated { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class StatisticsSummary
    {
        public int TotalVoters { get; set; }
        public int CastedVotes { get; set; }
        public decimal TurnoutPercentage { get; set; }
    }

    public class StatisticsBreakdown
    {
        public GenderStatistics Male { get; set; }
        public GenderStatistics Female { get; set; }
        public GenderStatistics Other { get; set; }
    }

    public class GenderStatistics
    {
        public int TotalVoters { get; set; }
        public int CastedVotes { get; set; }
        public decimal TurnoutPercentage { get; set; }
    }

    /// <summary>
    /// Update operation result
    /// </summary>
    public class UpdateStatisticsResult
    {
        public string Result { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
