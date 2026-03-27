using System.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace SmkcApi.Repositories
{
    public interface IOracleConnectionFactory
    {
        // Legacy default connection
        OracleConnection Create();
        OracleConnection CreateWS();
        // New dual-schema connections
        OracleConnection CreateUlberp();
        OracleConnection CreateAbas();
        OracleConnection CreateWebsite();
    }

    public class OracleConnectionFactory : IOracleConnectionFactory
    {
        private readonly string _defaultCs;

        // Backward-compatible constructor, allows specifying a config name
        public OracleConnectionFactory(string connectionStringName = "OracleDbAbas")
        {
            _defaultCs = ConfigurationManager.ConnectionStrings[connectionStringName]?.ConnectionString
                          ?? ConfigurationManager.ConnectionStrings["OracleDbAbas"].ConnectionString;
        }

        // Legacy default Create() will use ABAS unless explicitly overridden by constructor
        public OracleConnection Create() => new OracleConnection(_defaultCs);

        public OracleConnection CreateUlberp()
        {
            var cs = ConfigurationManager.ConnectionStrings["OracleDbUlberp"].ConnectionString;
            return new OracleConnection(cs);
        }
        public OracleConnection CreateWS()
        {
            var cs = ConfigurationManager.ConnectionStrings["OracleDb"].ConnectionString;
            return new OracleConnection(cs);
        }

        public OracleConnection CreateAbas()
        {
            var cs = ConfigurationManager.ConnectionStrings["OracleDbAbas"].ConnectionString;
            return new OracleConnection(cs);
        }

        public OracleConnection CreateWebsite()
        {
            var cs = ConfigurationManager.ConnectionStrings["OracleDbWebsite"].ConnectionString;
            return new OracleConnection(cs);
        }
    }
}
