using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using SmkcApi.Repositories;
using SmkcApi.Services;
using SmkcApi.Controllers.DepositManager;
using SmkcApi.Repositories.DepositManager;
using SmkcApi.Services.DepositManager;
using SmkcApi.Controllers; // for VotersController and CoreAccountController
using SmkcApi.Controllers.BoothMapping; // for Booth Mapping controllers
using SmkcApi.Repositories.BoothMapping; // for Booth Mapping repositories
using SmkcApi.Services.BoothMapping; // for Booth Mapping services
using SmkcApi.Controllers.VotingStatistics; // for Voting Statistics controller
using SmkcApi.Repositories.VotingStatistics; // for Voting Statistics repository

namespace SmkcApi.App_Start
{
    /// <summary>
    /// Simple dependency resolver for demonstration purposes.
    /// In production, use a proper DI container like Unity, Autofac, or Ninject.
    /// </summary>
    public partial class SimpleDependencyResolver : IDependencyResolver
    {
        private readonly IDictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();

        public SimpleDependencyResolver()
        {
            RegisterDependencies();
            RegisterDepositManager();
            RegisterDuplicateVoters();
            RegisterAuth();
            RegisterBoothMapping(); // New registration for booth mapping
            RegisterVotingStatistics(); // New registration for voting statistics
            RegisterWcwcDisability();
        }

        /// <summary>
        /// Central place to register all dependencies.
        /// </summary>
        private void RegisterDependencies()
        {
            // Default connection factory -> ABAS for business ops
            _factories[typeof(IOracleConnectionFactory)] = () => new OracleConnectionFactory("OracleDbAbas");
            _factories[typeof(SmkcApi.Repositories.IOracleConnectionFactory)] = () => new SmkcApi.Repositories.OracleConnectionFactory("OracleDbAbas");

            _factories[typeof(SmkcApi.Repositories.IWaterRepository)] = () =>
                new SmkcApi.Repositories.WaterRepository(
                    GetService(typeof(SmkcApi.Repositories.IOracleConnectionFactory)) as SmkcApi.Repositories.IOracleConnectionFactory
                );
            
            // Register SMS sender
            _factories[typeof(SmkcApi.Infrastructure.ISmsSender)] = () =>
                new SmkcApi.Infrastructure.SmsSender(
                    GetService(typeof(SmkcApi.Repositories.IWaterRepository)) as SmkcApi.Repositories.IWaterRepository
                );
            
            // Register SMS service
            _factories[typeof(SmkcApi.Services.ISmsService)] = () =>
                new SmkcApi.Services.SmsService(
                    GetService(typeof(SmkcApi.Repositories.IWaterRepository)) as SmkcApi.Repositories.IWaterRepository,
                    GetService(typeof(SmkcApi.Infrastructure.ISmsSender)) as SmkcApi.Infrastructure.ISmsSender
                );
            
            _factories[typeof(SmkcApi.Services.IWaterService)] = () =>
                new SmkcApi.Services.WaterService(
                    GetService(typeof(SmkcApi.Repositories.IWaterRepository)) as SmkcApi.Repositories.IWaterRepository
                );

            // Water Dashboard: uses WS schema (OracleDb connection string)
            var wsConnFactory = new SmkcApi.Repositories.OracleConnectionFactory("OracleDb");
            _factories[typeof(SmkcApi.Repositories.IWaterDashboardRepository)] = () =>
                new SmkcApi.Repositories.WaterDashboardRepository(wsConnFactory);
            _factories[typeof(SmkcApi.Services.IWaterDashboardService)] = () =>
                new SmkcApi.Services.WaterDashboardService(
                    GetService(typeof(SmkcApi.Repositories.IWaterDashboardRepository)) as SmkcApi.Repositories.IWaterDashboardRepository
                );

            _factories[typeof(SmkcApi.Controllers.WaterController)] = () =>
                new SmkcApi.Controllers.WaterController(
                    GetService(typeof(SmkcApi.Services.IWaterService)) as SmkcApi.Services.IWaterService,
                    GetService(typeof(SmkcApi.Services.ISmsService)) as SmkcApi.Services.ISmsService,
                    GetService(typeof(SmkcApi.Services.IWaterDashboardService)) as SmkcApi.Services.IWaterDashboardService
                );

            _factories[typeof(IAccountRepository)] = () => new AccountRepository();
            _factories[typeof(ICustomerRepository)] = () => new CustomerRepository();
            _factories[typeof(ITransactionRepository)] = () => new TransactionRepository();

            _factories[typeof(SmkcApi.Services.IAccountService)] = () => new SmkcApi.Services.AccountService(
                GetService(typeof(IAccountRepository)) as IAccountRepository,
                GetService(typeof(ICustomerRepository)) as ICustomerRepository
            );
            _factories[typeof(ICustomerService)] = () => new CustomerService(
                GetService(typeof(ICustomerRepository)) as ICustomerRepository
            );
            _factories[typeof(ITransactionService)] = () => new TransactionService(
                GetService(typeof(ITransactionRepository)) as ITransactionRepository,
                GetService(typeof(IAccountRepository)) as IAccountRepository
            );

            _factories[typeof(SmkcApi.Controllers.CoreAccountController)] = () => new SmkcApi.Controllers.CoreAccountController(
                GetService(typeof(SmkcApi.Services.IAccountService)) as SmkcApi.Services.IAccountService
            );
        }

        private void RegisterDuplicateVoters()
        {
            // Voter module must use WS connection string (OracleDb)
            var voterConnFactory = new SmkcApi.Repositories.OracleConnectionFactory("OracleDb");

            _factories[typeof(IVoterRepository)] = () => new VoterRepository(
                voterConnFactory
            );
            _factories[typeof(IVoterService)] = () => new VoterService(
                GetService(typeof(IVoterRepository)) as IVoterRepository
            );
            _factories[typeof(VotersController)] = () => new VotersController(
                GetService(typeof(IVoterService)) as IVoterService
            );
        }

        private void RegisterDepositManager()
        {
            _factories[typeof(IDepositRepository)] = () => new DepositRepository(
                GetService(typeof(IOracleConnectionFactory)) as IOracleConnectionFactory
            );

            // Storage service for consent documents - supports both FTP and Network share
            var storageType = System.Configuration.ConfigurationManager.AppSettings["Storage_Type"] ?? "ftp";
            
            if (storageType.Equals("network", StringComparison.OrdinalIgnoreCase))
            {
                // Use network share storage (recommended for internal LAN)
                _factories[typeof(IFtpStorageService)] = () => new NetworkStorageService();
                System.Diagnostics.Trace.TraceInformation("Storage configured: Network Share");
            }
            else
            {
                // Use FTP storage (fallback)
                _factories[typeof(IFtpStorageService)] = () => new FtpStorageService();
                System.Diagnostics.Trace.TraceInformation("Storage configured: FTP");
            }

            _factories[typeof(IBankService)] = () => new BankService(
                GetService(typeof(IDepositRepository)) as IDepositRepository,
                GetService(typeof(IFtpStorageService)) as IFtpStorageService
            );
            _factories[typeof(SmkcApi.Services.DepositManager.IAccountService)] = () => new SmkcApi.Services.DepositManager.AccountService(
                GetService(typeof(IDepositRepository)) as IDepositRepository,
                GetService(typeof(IFtpStorageService)) as IFtpStorageService
            );
            _factories[typeof(ICommissionerService)] = () => new CommissionerService(
                GetService(typeof(IDepositRepository)) as IDepositRepository,
                GetService(typeof(IFtpStorageService)) as IFtpStorageService
            );

            _factories[typeof(BankController)] = () => new BankController(
                GetService(typeof(IBankService)) as IBankService
            );
            _factories[typeof(SmkcApi.Controllers.DepositManager.AccountController)] = () => new SmkcApi.Controllers.DepositManager.AccountController(
                GetService(typeof(SmkcApi.Services.DepositManager.IAccountService)) as SmkcApi.Services.DepositManager.IAccountService
            );
            _factories[typeof(CommissionerController)] = () => new CommissionerController(
                GetService(typeof(ICommissionerService)) as ICommissionerService
            );
            
            // Consent Document Controller (common endpoint for all roles)
            _factories[typeof(ConsentDocumentController)] = () => new ConsentDocumentController(
                GetService(typeof(IFtpStorageService)) as IFtpStorageService
            );
            
            // FTP Diagnostic Controller (for troubleshooting network routing)
            _factories[typeof(FtpDiagnosticController)] = () => new FtpDiagnosticController();
        }

        /// <summary>
        /// Register authentication dependencies (AuthController, AuthService, AuthRepository)
        /// Auth uses ULBERP connection string for user authentication
        /// </summary>
        private void RegisterAuth()
        {
            // Auth module must use ULBERP connection string
            var authConnFactory = new SmkcApi.Repositories.OracleConnectionFactory("OracleDbUlberp");

            _factories[typeof(IAuthRepository)] = () => new AuthRepository(authConnFactory);
            _factories[typeof(IAuthService)] = () => new AuthService(
                GetService(typeof(IAuthRepository)) as IAuthRepository
            );
            _factories[typeof(AuthController)] = () => new AuthController(
                GetService(typeof(IAuthService)) as IAuthService
            );
        }

        /// <summary>
        /// Register booth mapping dependencies (controllers, services, repositories)
        /// Auth uses ULBERP connection string for user authentication
        /// Booth operations use WEBSITE (ws/ws) connection string
        /// </summary>
        private void RegisterBoothMapping()
        {
            // Booth Auth Repository - uses ULBERP schema for authentication
            _factories[typeof(IBoothAuthRepository)] = () => new BoothAuthRepository();

            // Booth Repository - uses WEBSITE schema for booth operations
            _factories[typeof(IBoothRepository)] = () => new BoothRepository();

            // Booth Auth Service
            _factories[typeof(IBoothAuthService)] = () => new BoothAuthService(
                GetService(typeof(IBoothAuthRepository)) as IBoothAuthRepository
            );

            // Booth Mapping Service
            _factories[typeof(IBoothMappingService)] = () => new BoothMappingService(
                GetService(typeof(IBoothRepository)) as IBoothRepository
            );

            // Booth Auth Controller
            _factories[typeof(BoothAuthController)] = () => new BoothAuthController(
                GetService(typeof(IBoothAuthService)) as IBoothAuthService
            );

            // Booth Mapping Controller
            _factories[typeof(BoothMappingController)] = () => new BoothMappingController(
                GetService(typeof(IBoothMappingService)) as IBoothMappingService
            );
        }

        /// <summary>
        /// Register voting statistics dependencies
        /// Uses WEBSITE connection string (website/website)
        /// Requires SHA-256 authentication
        /// </summary>
        private void RegisterVotingStatistics()
        {
            // Voting Statistics Repository - uses WEBSITE schema
            _factories[typeof(IVotingStatisticsRepository)] = () => new VotingStatisticsRepository(
                GetService(typeof(IOracleConnectionFactory)) as IOracleConnectionFactory
            );

            // Voting Statistics Controller
            _factories[typeof(VotingStatisticsController)] = () => new VotingStatisticsController(
                GetService(typeof(IVotingStatisticsRepository)) as IVotingStatisticsRepository
            );
        }

        private void RegisterWcwcDisability()
        {
            var wcwcConnFactory = new SmkcApi.Repositories.OracleConnectionFactory("OracleDbWcwc");

            _factories[typeof(IWcwcDisabilityRepository)] = () => new WcwcDisabilityRepository(wcwcConnFactory);
            _factories[typeof(IWcwcDocumentStorageService)] = () => new WcwcDocumentStorageService();
            _factories[typeof(IWcwcDisabilityService)] = () => new WcwcDisabilityService(
                GetService(typeof(IWcwcDisabilityRepository)) as IWcwcDisabilityRepository,
                GetService(typeof(IWcwcDocumentStorageService)) as IWcwcDocumentStorageService
            );
            _factories[typeof(WomenChildWelfareController)] = () => new WomenChildWelfareController(
                GetService(typeof(IWcwcDisabilityService)) as IWcwcDisabilityService
            );
        }

        public object GetService(Type serviceType)
        {
            return _factories.ContainsKey(serviceType) ? _factories[serviceType]() : null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            var service = GetService(serviceType);
            return service != null ? new[] { service } : new object[0];
        }

        public IDependencyScope BeginScope()
        {
            return new SimpleDependencyScope(this);
        }

        public void Dispose()
        {
            _factories.Clear();
        }
    }

    /// <summary>
    /// Simple dependency scope implementation
    /// </summary>
    public class SimpleDependencyScope : IDependencyScope
    {
        private readonly SimpleDependencyResolver _resolver;

        public SimpleDependencyScope(SimpleDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        public object GetService(Type serviceType)
        {
            return _resolver.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _resolver.GetServices(serviceType);
        }

        public void Dispose()
        {
            // Nothing to dispose in this simple implementation
        }
    }
}
