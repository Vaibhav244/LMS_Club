using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace LMSAPI_ATTENDANCE.Repository
{
    public class DbLayer
    {
        private readonly string _connectionString;

        public DbLayer(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Get employee count
        public int GetEmployeeCount(string Query)
        {
            int count = 0;
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(Query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    count = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            return count;
        }

        // Get working location
        public string GetWorkingLocation(string Query)
        {
            string location = "";
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(Query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    location = Convert.ToString(cmd.ExecuteScalar());
                }
            }
            return location;
        }

        // Count EL manually updated rows
        public int noOfRowsELManuallyUpdated(string Query)
        {
            int count = 0;
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(Query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    count = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            return count;
        }
    }
}