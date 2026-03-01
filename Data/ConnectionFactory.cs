using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;

namespace AduanaDigital.Data
{
    public class ConnectionFactory
    {
        private static IConfiguration _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static IDbConnection GetConnection
        {
            get
            {
                var sqlConnection = new SqlConnection();

                if (sqlConnection == null) return null;

                sqlConnection.ConnectionString = _configuration.GetConnectionString("DBConnection_PlanSalud");
                sqlConnection.Open();

                return sqlConnection;
            }
        }
    }
}