using SmkcApi.Models.DepositManager;

namespace SmkcApi.Services.DepositManager
{
    public interface IBankService
    {
        ApiResponse GetRequirements(string status, string depositType);
        ApiResponse GetRequirementById(string id);
        ApiResponse GetQuotes(string bankId, string requirementId);
        ApiResponse SubmitQuote(SubmitQuoteRequest request);
        ApiResponse GetConsentDocument(string quoteId, string requirementId, string bankId);
        ApiResponse GetDashboardStats(string bankId);
    }

    public interface IAccountService
    {
        ApiResponse GetRequirements(string status, string depositType);
        ApiResponse GetRequirementById(string id);
        ApiResponse CreateRequirement(CreateRequirementRequest request);
        ApiResponse PublishRequirement(string requirementId, string authorizedBy);
        ApiResponse DeleteRequirement(string requirementId);
        ApiResponse GetBanks(string status);
        ApiResponse CreateBank(CreateBankRequest request);
        ApiResponse GetQuotes(string requirementId);
        ApiResponse GetConsentDocument(string quoteId, string requirementId, string bankId);
        ApiResponse GetDashboardStats();
    }

    public interface ICommissionerService
    {
        ApiResponse GetRequirements(string status);
        ApiResponse GetRequirementWithQuotes(string requirementId);
        ApiResponse GetQuotes(string requirementId);
        ApiResponse AuthorizeRequirement(string requirementId, string commissionerId);
        ApiResponse FinalizeDeposit(string requirementId, string bankId);
        ApiResponse GetDashboardStats();
        ApiResponse GetEnhancedKpis(string bankId, string depositType, System.DateTime? fromDate, System.DateTime? toDate);
        ApiResponse GetBankWiseAnalytics(string depositType, System.DateTime? fromDate, System.DateTime? toDate);
        ApiResponse GetUpcomingMaturities(int? withinDays, string bankId, string depositType, int? minDaysLeft, int? maxDaysLeft, string schemeName);
        ApiResponse GetDepositTypeDistribution(string bankId, System.DateTime? fromDate, System.DateTime? toDate);
        ApiResponse GetInterestTimeline(int? monthsAhead, string bankId, string depositType, System.DateTime? fromDate, System.DateTime? toDate);
        ApiResponse GetPortfolioHealth(int? minDiversifiedBanks, decimal? maxSingleBankPercent, decimal? targetAvgRate, string bankId, string depositType, System.DateTime? fromDate, System.DateTime? toDate);
        ApiResponse GetConsentDocument(string quoteId, string requirementId, string bankId);
    }
}
