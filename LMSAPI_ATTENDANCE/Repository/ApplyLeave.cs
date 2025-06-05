using System;
using System.Data;
using System.Threading.Tasks;
using LMSAPI_ATTENDANCE.Model;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace LMSAPI_ATTENDANCE.Repository
{
    public class ApplyLeave
    {
        private readonly string _connectionString;

        public ApplyLeave(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Insert leave application
        public async Task<string> InsertLeaveAsync(Leave leave)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    string sql = "INSERT INTO ts2_emp_leave_details(TS2_EMP_LEAVE_DETAILS_ID, EMP_ID, LEAVE_CAT_DESC_ID, LEAVE_START_DATE, LEAVE_END_DATE,LEAVE_APPLIED_DATE, STATUS, COMMENTS, PETO_TAKEN_FROM_CURRENT) " +
                           "VALUES (ts2_emp_leave_details_SEQ.NEXTVAL, :empId, :leaveTypeId, :startDate, :endDate, SYSDATE, :status, :comments, :attenCount) " +
                           "RETURNING TS2_EMP_LEAVE_DETAILS_ID INTO :newId";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.CommandType = CommandType.Text;

                        TimeSpan difference = leave.end_date - leave.start_date;
                        int ATTENDENCE_COUNT = 0;
                        if (leave.includeHolidayWeekoff != 1 && (difference.Days + 1) != ATTENDENCE_COUNT)
                        {
                            return "Leave can be applied only for the working days";
                        }
                        else if (ATTENDENCE_COUNT > 0)
                        {
                            cmd.Parameters.Add("empId", OracleDbType.Int32).Value = leave.emp_id;
                            cmd.Parameters.Add("leaveTypeId", OracleDbType.Int32).Value = leave.leave_type_id;
                            cmd.Parameters.Add("startDate", OracleDbType.Date).Value = leave.start_date;
                            cmd.Parameters.Add("endDate", OracleDbType.Date).Value = leave.end_date;
                            cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = "Pending";
                            cmd.Parameters.Add("comments", OracleDbType.Varchar2).Value = leave.reason;
                            cmd.Parameters.Add("attenCount", OracleDbType.Int32).Value = ATTENDENCE_COUNT;
                            var outputParam = new OracleParameter("newId", OracleDbType.Int32)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(outputParam);

                            conn.Open();
                            cmd.ExecuteNonQuery();
                            string newId = Convert.ToString(outputParam.Value);
                            return newId;
                        }
                        else
                        {
                            return "Issue";
                        }
                    }
                }
            }
            catch
            {
                return "Issue";
            }
        }
    }
}