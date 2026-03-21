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
                // Clave alineada con appsettings.json
                var connectionString = _configuration.GetConnectionString("DBConnection_AduanaDigital");

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException(
                        "La cadena de conexión 'DBConnection_AduanaDigital' no está configurada en appsettings.json.");

                var sqlConnection = new SqlConnection(connectionString);

                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

                return sqlConnection;
            }
        }
    }
}