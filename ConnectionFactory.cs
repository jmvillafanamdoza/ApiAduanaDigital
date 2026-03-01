namespace Crp.Ps.Scheduler.Da.Facturacion.Electronica
{
    public class ConnectionFactory
    {
        public static IDbConnection GetConnection
        {
            get
            {
                var sqlConnection = new SqlConnection();
                
                if (sqlConnection == null) return null;
                
                sqlConnection.ConnectionString = ConfigurationManager.ConnectionStrings["DBConnection_PlanSalud"].ConnectionString;
                sqlConnection.Open();
                
                return sqlConnection;
            }
        }
    }
}