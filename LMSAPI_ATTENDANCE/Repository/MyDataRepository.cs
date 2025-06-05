using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LMSAPI_ATTENDANCE.Model;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace LMSAPI_ATTENDANCE.Repository
{
    public class MyDataRepository
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public MyDataRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        #region Employee Details and Attendance

        // Get employee details
        public async Task<IEnumerable<Employee>> GetEmployeeDetailsAsync(int EMPID)
        {
            var dataList = new List<Employee>();
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                using (OracleCommand cmd = new OracleCommand("SELECT EMP_NAME, CITY, STATE, POSTAL_CODE, COUNTRY, EMP_STATUS, EMP_LEVEL, COSTCENTER, JOB_TITLE_FULL, SUPERVISORNAME, MANAGERNAME, DIRECTORNAME, SENIORDIRECTORNAME, JOB_GRADE FROM TS2_EMPLOYEE_DETAILS WHERE EMP_ID = '" + EMPID + "'", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    await conn.OpenAsync();
                    using (OracleDataReader reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var data = new Employee
                            {
                                EMP_NAME = Convert.ToString(reader["EMP_NAME"]),
                                CITY = Convert.ToString(reader["CITY"]),
                                STATE = Convert.ToString(reader["STATE"]),
                                POSTAL_CODE = Convert.ToString(reader["POSTAL_CODE"]),
                                COUNTRY = Convert.ToString(reader["COUNTRY"]),
                                EMP_STATUS = Convert.ToString(reader["EMP_STATUS"]),
                                EMP_LEVEL = Convert.ToString(reader["EMP_LEVEL"]),
                                COSTCENTER = Convert.ToString(reader["COSTCENTER"]),
                                JOB_TITLE_FULL = Convert.ToString(reader["JOB_TITLE_FULL"]),
                                SUPERVISORNAME = Convert.ToString(reader["SUPERVISORNAME"]),
                                MANAGERNAME = Convert.ToString(reader["MANAGERNAME"]),
                                DIRECTORNAME = Convert.ToString(reader["DIRECTORNAME"]),
                                SENIORDIRECTORNAME = Convert.ToString(reader["SENIORDIRECTORNAME"]),
                                JOB_GRADE = Convert.ToString(reader["JOB_GRADE"])
                            };
                            dataList.Add(data);
                        }
                    }
                }
            }
            return dataList;
        }

        // Get attendance (skeleton, actual population missing)
        public async Task<IEnumerable<Employee>> GetAttendanceAsync(int EMPID, string START_DATE, string END_DATE)
        {
            string dateStr1 = START_DATE;
            DateTime date1 = DateTime.ParseExact(dateStr1, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            string StartDateformattedDate = date1.ToString("MM/dd/yyyy");

            string dateStr2 = END_DATE;
            DateTime date2 = DateTime.ParseExact(dateStr2, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            string EndDateformattedDate = date2.ToString("MM/dd/yyyy");
            START_DATE = StartDateformattedDate;
            END_DATE = EndDateformattedDate;

            var dataList = new List<Employee>();
            DataTable dt = new DataTable();

            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                string sql = "..."; // Query omitted for brevity
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add("EMPID", OracleDbType.Int32).Value = EMPID;
                    conn.Open();
                    using (OracleDataAdapter adapter = new OracleDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            return dataList;
        }

        // Insert attendance record
        public async Task<string> InsertAttendanceAsync(Attendance attendance)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    string sql = "INSERT INTO RM_ATTENDANCE_DETAILS(ATTENDANCE_DETAILS_ID, EMP_ID, ATTENDANCE_TYPE_ID, START_DATE, END_DATE, APPLIED_DATE, STATUS, COMMENTS, ATTEN_COUNT) " +
                           "VALUES (RM_ATTENDANCE_DETAILS_SEQ.NEXTVAL, :empId, :attendanceTypeId, :startDate, :endDate, SYSDATE, :status, :comments, :attenCount) " +
                           "RETURNING ATTENDANCE_DETAILS_ID INTO :newId";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.CommandType = CommandType.Text;

                        TimeSpan difference = attendance.end_date - attendance.start_date;
                        int ATTENDENCE_COUNT = GetWorkingDaysBetween(attendance.start_date, attendance.end_date, attendance.emp_id, attendance.IncludeHolidayWeekoff);
                        if (attendance.IncludeHolidayWeekoff != 1 && (difference.Days + 1) != ATTENDENCE_COUNT)
                        {
                            return "Attendance can be applied only for the working days";
                        }
                        else if (ATTENDENCE_COUNT > 0)
                        {
                            cmd.Parameters.Add("empId", OracleDbType.Int32).Value = attendance.emp_id;
                            cmd.Parameters.Add("attendanceTypeId", OracleDbType.Int32).Value = attendance.attendance_type_id;
                            cmd.Parameters.Add("startDate", OracleDbType.Date).Value = attendance.start_date;
                            cmd.Parameters.Add("endDate", OracleDbType.Date).Value = attendance.end_date;
                            cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = "Pending";
                            cmd.Parameters.Add("comments", OracleDbType.Varchar2).Value = attendance.comments;
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

        #endregion

        #region Leave Management

        public async Task<bool> IsSupervisorOnshoreAsync(int empId)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    string query = @"
                SELECT COUNT(sup.emp_id) 
                FROM ts2_employee_details t 
                INNER JOIN ts2_employee_details sup ON t.manager_id = sup.emp_id
                WHERE UPPER(sup.email_id_off) LIKE 'X%' AND t.emp_id = :empId";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add("empId", OracleDbType.Int32).Value = empId;

                        await conn.OpenAsync();
                        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception using your logging framework
                throw new Exception($"Error checking supervisor onshore status for employee {empId}", ex);
            }
        }

        public async Task<string> InsertLeaveWithStatusAsync(Leave leave, string status)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    string sql = @"INSERT INTO ts2_emp_leave_details(
                TS2_EMP_LEAVE_DETAILS_ID, 
                EMP_ID, 
                LEAVE_CAT_DESC_ID, 
                LEAVE_START_DATE, 
                LEAVE_END_DATE,
                LEAVE_APPLIED_DATE, 
                STATUS, 
                COMMENTS, 
                PETO_TAKEN_FROM_CURRENT,
                PETO_TAKEN_FROM_HELD,
                LEAVE_INCLUDES_HOLIDAY
            ) VALUES (
                ts2_emp_leave_details_SEQ.NEXTVAL, 
                :empId, 
                :leaveTypeId, 
                :startDate, 
                :endDate, 
                SYSDATE, 
                :status, 
                :comments, 
                :attenCount,
                :petoFromHeld,
                :includeHoliday
            ) RETURNING TS2_EMP_LEAVE_DETAILS_ID INTO :newId";

                    if (LeaveValidation(leave.emp_id, leave.start_date, leave.end_date) != "")
                    {
                        return "According to the leave policy, combining PTO and sick leave is not permitted. Please submit HR ticket along with an approval email from your Manager for further review";
                    }
                    else
                    {
                        using (OracleCommand cmd = new OracleCommand(sql, conn))
                        {
                            cmd.CommandType = CommandType.Text;

                            TimeSpan difference = leave.end_date - leave.start_date;
                            double LEAVE_COUNT = await CalculateWorkingDays(leave.emp_id, leave.start_date, leave.end_date, leave.includeHolidayWeekoff == 1);

                            // Handle half-day leave
                            if (leave.helf_leave == 1)
                            {
                                LEAVE_COUNT = 0.5;
                            }

                            if (leave.includeHolidayWeekoff != 1 && (difference.Days + 1) != LEAVE_COUNT)
                            {
                                return "Leave can be applied only for the working days";
                            }
                            else if (LEAVE_COUNT > 0)
                            {
                                cmd.Parameters.Add("empId", OracleDbType.Int32).Value = leave.emp_id;
                                cmd.Parameters.Add("leaveTypeId", OracleDbType.Int32).Value = leave.leave_type_id;
                                cmd.Parameters.Add("startDate", OracleDbType.Date).Value = leave.start_date;
                                cmd.Parameters.Add("endDate", OracleDbType.Date).Value = leave.end_date;
                                cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = status; // Use the provided status
                                cmd.Parameters.Add("comments", OracleDbType.Varchar2).Value = leave.reason ?? string.Empty;
                                cmd.Parameters.Add("attenCount", OracleDbType.Decimal).Value = 0;

                                double petoFromHeld = 0;
                                cmd.Parameters.Add("petoFromHeld", OracleDbType.Decimal).Value = 0;

                                cmd.Parameters.Add("includeHoliday", OracleDbType.Int32).Value = leave.includeHolidayWeekoff;

                                var outputParam = new OracleParameter("newId", OracleDbType.Int32)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(outputParam);

                                await conn.OpenAsync();
                                await cmd.ExecuteNonQueryAsync();
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
            }
            catch (Exception e)
            {
                return "Issue";
            }
        }

        public async Task<string> InsertLeaveAsync(Leave leave)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    
                    string sql = @"INSERT INTO ts2_emp_leave_details(
        TS2_EMP_LEAVE_DETAILS_ID, 
        EMP_ID, 
        LEAVE_CAT_DESC_ID, 
        LEAVE_START_DATE, 
        LEAVE_END_DATE,
        LEAVE_APPLIED_DATE, 
        STATUS, 
        COMMENTS, 
        PETO_TAKEN_FROM_CURRENT,
        PETO_TAKEN_FROM_HELD,
        LEAVE_INCLUDES_HOLIDAY,MEDICLE_DOCUMENT
    ) VALUES (
        ts2_emp_leave_details_SEQ.NEXTVAL, 
        :empId, 
        :leaveTypeId, 
        :startDate, 
        :endDate, 
        SYSDATE, 
        :status, 
        :comments, 
        :attenCount,
        :petoFromHeld,
        :includeHoliday, :medicleDocument
    ) RETURNING TS2_EMP_LEAVE_DETAILS_ID INTO :newId";

                    if (LeaveValidation(leave.emp_id, leave.start_date, leave.end_date) != "")
                    {
                        return "According to the leave policy, combining PTO and sick leave is not permitted. Please submit HR ticket along with an approval email from your Manager for further review";
                    }
                    else
                    {
                        using (OracleCommand cmd = new OracleCommand(sql, conn))
                        {
                            cmd.CommandType = CommandType.Text;

                            TimeSpan difference = leave.end_date - leave.start_date;
                            double LEAVE_COUNT = await CalculateWorkingDays(leave.emp_id, leave.start_date, leave.end_date, leave.includeHolidayWeekoff == 1);

                            // Handle half-day leave
                            if (leave.helf_leave == 1)
                            {
                                LEAVE_COUNT = 0.5;
                            }

                            if (leave.includeHolidayWeekoff != 1 && (difference.Days + 1) != LEAVE_COUNT)
                            {
                                return "Leave can be applied only for the working days";
                            }
                            else if (LEAVE_COUNT > 0)
                            {
                                cmd.Parameters.Add("empId", OracleDbType.Int32).Value = leave.emp_id;
                                cmd.Parameters.Add("leaveTypeId", OracleDbType.Int32).Value = leave.leave_type_id;
                                cmd.Parameters.Add("startDate", OracleDbType.Date).Value = leave.start_date;
                                cmd.Parameters.Add("endDate", OracleDbType.Date).Value = leave.end_date;
                                cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = "Pending";
                                cmd.Parameters.Add("comments", OracleDbType.Varchar2).Value = leave.reason ?? string.Empty;
                                cmd.Parameters.Add("attenCount", OracleDbType.Decimal).Value = 0;

                                cmd.Parameters.Add("petoFromHeld", OracleDbType.Decimal).Value = 0;

                                cmd.Parameters.Add("includeHoliday", OracleDbType.Int32).Value = leave.includeHolidayWeekoff;
                                cmd.Parameters.Add("medicleDocument", OracleDbType.Varchar2).Value = leave.medicaldocument;

                                var outputParam = new OracleParameter("newId", OracleDbType.Int32)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(outputParam);

                                await conn.OpenAsync();
                                await cmd.ExecuteNonQueryAsync();
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
            }
            catch (Exception e)
            {
                return "Issue";
            }
        }
        public string LeaveValidation(int iEmpId, DateTime dtStart, DateTime dtEnd)
        {
            try
            {
                double noOfDaysSickLeaveTakenPreAndPost = 0;
                List<DateTime> lstDateTimes = GetFirstAndLastDatesOfPeriod(iEmpId, dtStart, dtEnd);
                noOfDaysSickLeaveTakenPreAndPost = GetSickLeavesTakenFromCurrentYear(iEmpId, lstDateTimes[0].AddDays(-1), lstDateTimes[1].AddDays(1));

                if (noOfDaysSickLeaveTakenPreAndPost > 0)
                {
                    return "According to the leave policy, combining PTO and sick leave is not permitted. Please submit HR ticket along with an approval email from your Manager for further review ";
                }
                else
                {
                    return "";
                }
            }
            catch
            {
                return "Error validating leave request. Please try again.";
            }
        }

        // Get first and last date for period
        public List<DateTime> GetFirstAndLastDatesOfPeriod(int empId, DateTime dtFirst, DateTime dtSecond)
        {
            try
            {
                List<DateTime> lstDates = new List<DateTime>();
                int iWeekend = 71;
                bool IsAShiftEmployee = false;
                string workingLocation = GetEmployeeWorkingLocation(empId);
                IsAShiftEmployee = IsShiftEmployee(empId);

                DataSet dsTimeSwipe = GetCompleteSwipeDetails(empId, UserType.Employee, dtFirst.AddDays(-10), dtSecond.AddDays(10));
                DataSet dsTempTimeSwipe = GetCompleteTempDetails(empId, UserType.Employee, dtFirst.AddDays(-10), dtSecond.AddDays(10));
                DataSet dsReason = GetReasonDetails(empId, UserType.Employee, dtFirst.AddDays(-10), dtSecond.AddDays(10));
                DataSet dsTempCardEntryDetails = GetCompleteTempCardEntryDetailsForLOPService(empId, UserType.Employee, dtFirst.AddDays(-10), dtSecond.AddDays(10));
                List<DataRow> dlTimeSwipe = new List<DataRow>(dsTimeSwipe.Tables[0].AsEnumerable());
                List<DataRow> dlTempTimeSwipe = new List<DataRow>(dsTempTimeSwipe.Tables[0].AsEnumerable());
                List<DataRow> dlReason = new List<DataRow>(dsReason.Tables[0].AsEnumerable());
                List<DataRow> dlTempCardEntryDetails = new List<DataRow>(dsTempCardEntryDetails.Tables[0].AsEnumerable());
                EmployeeAttendanceDTO employeeAttendanceDTO = new EmployeeAttendanceDTO();

                if (IsAShiftEmployee)
                {
                    DataSet dsWeekend = new DataSet();
                    DataRow dRow;
                    dsWeekend = GetWeekendForShiftEmployees(empId, dtFirst, dtSecond);

                    for (int iCount = 0; iCount < dsWeekend.Tables[0].Rows.Count; iCount++)
                    {
                        dRow = dsWeekend.Tables[0].Rows[iCount];
                        iWeekend = Convert.ToInt32(dRow["WEEKEND"]);
                    }
                }
                // Check left boundary
                while (isHoliday(dtFirst.AddDays(-1), workingLocation) || IsEmplolyeeHasWeeklyOffForTheSelectedDate(Convert.ToString(iWeekend), Convert.ToString(Convert.ToInt32(dtFirst.AddDays(-1).DayOfWeek) + 1)))
                {
                    employeeAttendanceDTO.SwipeDetails = GetSwipeDetailForParticularDateForLOPService(empId, dtFirst.AddDays(-1), dlTimeSwipe);
                    if (employeeAttendanceDTO.SwipeDetails.IsDataPresent)
                        break;

                    employeeAttendanceDTO.TempCardDetail = GetTempCardDetailForParticularDateForLOPService(empId, dtFirst.AddDays(-1), dlTempTimeSwipe);
                    if (employeeAttendanceDTO.TempCardDetail.IsDataPresent)
                        break;

                    employeeAttendanceDTO.ReasonDetails = GetReasonDetailForSelectedDateForLOPService(empId, dtFirst.AddDays(-1), dlReason);
                    if (employeeAttendanceDTO.ReasonDetails.IsReasonPresent)
                        break;

                    employeeAttendanceDTO.TempCardEntryDetail = GetTempCardEntryDetailForParticularDateForLOPService(empId, dtFirst.AddDays(-1), dlTempCardEntryDetails);
                    if (employeeAttendanceDTO.TempCardEntryDetail.IsDataPresent)
                        break;
                    dtFirst = dtFirst.AddDays(-1);
                }
                // Check right boundary
                while (isHoliday(dtSecond.AddDays(1), workingLocation) || IsEmplolyeeHasWeeklyOffForTheSelectedDate(Convert.ToString(iWeekend), Convert.ToString(Convert.ToInt32(dtSecond.AddDays(1).DayOfWeek) + 1)))
                {
                    employeeAttendanceDTO.SwipeDetails = GetSwipeDetailForParticularDateForLOPService(empId, dtSecond.AddDays(1), dlTimeSwipe);
                    if (employeeAttendanceDTO.SwipeDetails.IsDataPresent)
                        break;

                    employeeAttendanceDTO.TempCardDetail = GetTempCardDetailForParticularDateForLOPService(empId, dtSecond.AddDays(1), dlTempTimeSwipe);
                    if (employeeAttendanceDTO.TempCardDetail.IsDataPresent)
                        break;

                    employeeAttendanceDTO.ReasonDetails = GetReasonDetailForSelectedDateForLOPService(empId, dtSecond.AddDays(1), dlReason);
                    if (employeeAttendanceDTO.ReasonDetails.IsReasonPresent)
                        break;

                    employeeAttendanceDTO.TempCardEntryDetail = GetTempCardEntryDetailForParticularDateForLOPService(empId, dtSecond.AddDays(1), dlTempCardEntryDetails);
                    if (employeeAttendanceDTO.TempCardEntryDetail.IsDataPresent)
                        break;
                    dtSecond = dtSecond.AddDays(1);
                }
                lstDates.Add(dtFirst);
                lstDates.Add(dtSecond);
                return lstDates;
            }
            catch
            {
                // Return default dates to prevent null reference
                return new List<DateTime> { dtFirst, dtSecond };
            }
        }

        // Get sick leaves taken
        public double GetSickLeavesTakenFromCurrentYear(int emp_Id, DateTime dFirst, DateTime toDate)
        {
            double sickLeavesTaken = 0;
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "begin :ret := RM_LEAVE_PKG.GETSICKLEAVESUMBETWEEN(:P_EMP_ID, :P_START_DATE, :P_END_DATE); end;";
                        cmd.CommandType = CommandType.Text;

                        cmd.Parameters.Add("ret", OracleDbType.Decimal).Direction = ParameterDirection.ReturnValue;
                        cmd.Parameters.Add("P_EMP_ID", OracleDbType.Int32).Value = emp_Id;
                        cmd.Parameters.Add("P_START_DATE", OracleDbType.Date).Value = dFirst;
                        cmd.Parameters.Add("P_END_DATE", OracleDbType.Date).Value = toDate;

                        conn.Open();
                        cmd.ExecuteNonQuery();

                        OracleDecimal sickLeaveSum = (OracleDecimal)cmd.Parameters["ret"].Value;
                        sickLeavesTaken = Convert.ToDouble(sickLeaveSum.Value);
                    }
                }
            }
            catch
            {
            }
            return sickLeavesTaken;
        }

        // Check if employee is shift
        public bool IsShiftEmployee(int empId)
        {
            string sql = "select count(*) from RM_SHIFT_EMPLOYEES WHERE STATUS <> 'CANCELLED' AND emp_id = " + empId;
            bool retVal = false;
            int iEmpCnt = 0;
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        conn.Open();
                        iEmpCnt = Convert.ToInt32(cmd.ExecuteScalar());
                        retVal = iEmpCnt > 0;
                    }
                }
            }
            catch
            {
            }
            return retVal;
        }

        // Get swipe details
        public DataSet GetCompleteSwipeDetails(int empId, UserType userType, DateTime dStart, DateTime dEnd)
        {
            int user_type = (int)userType;
            DataSet swipeDetails = new DataSet();
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand("TimeReport_pkg_modified.getSwipeDetails", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("empid", OracleDbType.Int32).Value = empId;
                        cmd.Parameters.Add("startDate", OracleDbType.Date).Value = new DateTime(dStart.Year, dStart.Month, dStart.Day, 0, 0, 1);
                        cmd.Parameters.Add("endDate", OracleDbType.Date).Value = new DateTime(dEnd.Year, dEnd.Month, dEnd.Day, 23, 59, 59);
                        cmd.Parameters.Add("dataTypeFlag", OracleDbType.Int32).Value = user_type;
                        cmd.Parameters.Add("CurEmpTimeReport", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        using (OracleDataAdapter adapter = new OracleDataAdapter(cmd))
                        {
                            conn.Open();
                            adapter.Fill(swipeDetails);
                        }
                    }
                }
            }
            catch
            {
            }
            return swipeDetails;
        }

        // Get temp swipe details
        public DataSet GetCompleteTempDetails(int empId, UserType userType, DateTime dStart, DateTime dEnd)
        {
            int user_type = (int)userType;
            DataSet swipeDetails = new DataSet();
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand("TimeReport_pkg_modified.getTempSwipeDetails", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("empid", OracleDbType.Int32).Value = empId;
                        cmd.Parameters.Add("startDate", OracleDbType.Date).Value = new DateTime(dStart.Year, dStart.Month, dStart.Day, 0, 0, 1);
                        cmd.Parameters.Add("endDate", OracleDbType.Date).Value = new DateTime(dEnd.Year, dEnd.Month, dEnd.Day, 23, 59, 59);
                        cmd.Parameters.Add("dataTypeFlag", OracleDbType.Int32).Value = user_type;
                        cmd.Parameters.Add("CurEmpTimeReport", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        using (OracleDataAdapter adapter = new OracleDataAdapter(cmd))
                        {
                            conn.Open();
                            adapter.Fill(swipeDetails);
                        }
                    }
                }
            }
            catch
            {
            }
            return swipeDetails;
        }

        // Get reason details
        public DataSet GetReasonDetails(int emp_Id, UserType userType, DateTime startDate, DateTime endDate)
        {
            DataSet ds = new DataSet();
            try
            {
                string sqlQuery = "..."; // Query omitted for brevity
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand(sqlQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        using (OracleDataAdapter adapter = new OracleDataAdapter(cmd))
                        {
                            conn.Open();
                            adapter.Fill(ds);
                        }
                    }
                }
            }
            catch
            {
            }
            return ds;
        }

        // Get temp card entry details for LOP
        public DataSet GetCompleteTempCardEntryDetailsForLOPService(int empId, UserType userType, DateTime dStart, DateTime dEnd)
        {
            DataSet ds = new DataSet();
            string sqlQuery = string.Empty;
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    if (userType == UserType.Employee)
                        sqlQuery = "SELECT employee_id as emp_id, temp_card_id, to_char(missed_date, 'MM/dd/yyyy') as DAY FROM rm_temp_card_details WHERE missed_date BETWEEN to_date('" + dStart.ToShortDateString() + "', 'MM/dd/yyyy') AND to_date('" + dEnd.ToShortDateString() + "', 'MM/dd/yyyy') AND employee_id =" + empId;
                    else
                        sqlQuery = "SELECT employee_id as emp_id, temp_card_id, to_char(missed_date, 'MM/dd/yyyy') as DAY FROM rm_temp_card_details WHERE missed_date BETWEEN to_date('" + dStart.ToShortDateString() + "', 'MM/dd/yyyy') AND to_date('" + dEnd.ToShortDateString() + "', 'MM/dd/yyyy')";

                    using (OracleCommand cmd = new OracleCommand(sqlQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        using (OracleDataAdapter adapter = new OracleDataAdapter(cmd))
                        {
                            conn.Open();
                            adapter.Fill(ds);
                        }
                    }
                }
            }
            catch
            {
            }
            return ds;
        }

        // Helper: swipe present for date
        public SwipeDetailsCollection GetSwipeDetailForParticularDateForLOPService(int emp_Id, DateTime date, List<DataRow> dlTimeSwipe)
        {
            SwipeDetailsCollection swipeCollection = new SwipeDetailsCollection();
            if (dlTimeSwipe.Any(dRow => dRow.Field<String>("DAY") == date.ToString("MM/dd/yyyy")))
                swipeCollection.IsDataPresent = true;
            return swipeCollection;
        }

        // Helper: temp swipe for date
        public SwipeDetailsCollection GetTempCardDetailForParticularDateForLOPService(int emp_Id, DateTime date, List<DataRow> dlTempTimeSwipe)
        {
            SwipeDetailsCollection swipeCollection = new SwipeDetailsCollection();
            if (dlTempTimeSwipe.Any(dRow => dRow.Field<String>("DAY") == date.ToString("MM/dd/yyyy")))
                swipeCollection.IsDataPresent = true;
            return swipeCollection;
        }

        // Helper: reason for date
        public ReasonDetailDTO GetReasonDetailForSelectedDateForLOPService(int emp_Id, DateTime date, List<DataRow> dlReason)
        {
            ReasonDetailDTO reasonDTO = new ReasonDetailDTO();
            if (dlReason.Any(reasonDetails => reasonDetails.Field<DateTime>("start_date") <= date && reasonDetails.Field<DateTime>("end_date") >= date))
                reasonDTO.IsReasonPresent = true;
            return reasonDTO;
        }

        // Helper: temp card entry for date
        public SwipeDetailsCollection GetTempCardEntryDetailForParticularDateForLOPService(int emp_Id, DateTime date, List<DataRow> dlTempCardEntryDetails)
        {
            SwipeDetailsCollection swipeCollection = new SwipeDetailsCollection();
            if (dlTempCardEntryDetails.Any(dRow => dRow.Field<String>("DAY") == date.ToString("MM/dd/yyyy")))
                swipeCollection.IsDataPresent = true;
            return swipeCollection;
        }

        // Form employee SQL condition
        private String FormEmployeeCondition(int rootEmpId, bool isManager)
        {
            if (rootEmpId == 0)
                return "";
            else if (isManager)
                return @" and t.emp_id in (SELECT emp_id FROM 
						(select emp_status,emp_id,emp_name,DECODE(emp_id,manager_id,null,manager_id)
						as manager_id FROM ts2_employee_details)  
						WHERE  EMP_STATUS = 'ACTIVE'
						and emp_id != " + rootEmpId +
                    " start with emp_id = " + rootEmpId +
                    " connect by prior emp_id = manager_id) ";
            else
                return " and t.emp_id = " + rootEmpId;
        }

        #endregion

        #region Working Days and Calendar Functions

        public bool AppliedDate(DateTime appliedDate)
        {
            int allowedDays = 6;
            var limitSetting = _configuration["LeaveSettings:CasualLeave:LeaveSettings:AppliedDateLimitDays"];
            if (!string.IsNullOrWhiteSpace(limitSetting) && int.TryParse(limitSetting, out int configLimit))
            {
                allowedDays = configLimit;
            }
            if (appliedDate.Date > DateTime.Today)
                return false;

            var daysDifference = (DateTime.Today - appliedDate.Date).TotalDays;

            return daysDifference <= allowedDays;
        }

        // Get weekends for shift employees
        private DataSet GetWeekendForShiftEmployees(int empId, DateTime dtFirst, DateTime dtSecond)
        {
            DataSet dsShift = new DataSet();
            string sql = "select * from RM_SHIFT_EMPLOYEES WHERE STATUS <> 'CANCELLED' AND emp_id = " + empId + "and "
                + " (shift_start_date between to_date('"
                + dtFirst.ToShortDateString() + "','mm/dd/yyyy')" + "and to_date('"
                + dtSecond.ToShortDateString() + "','mm/dd/yyyy')" + " OR "
                + "shift_start_date between to_date('"
                + dtFirst.ToShortDateString() + "','mm/dd/yyyy')" + "and to_date('"
                + dtSecond.ToShortDateString() + "','mm/dd/yyyy') OR "
                + "(to_date('" + dtFirst.ToShortDateString() + "','mm/dd/yyyy')"
                + "between shift_start_date and shift_end_date OR "
                + "to_date('" + dtSecond.ToShortDateString() + "','mm/dd/yyyy')"
                + "between shift_start_date and shift_end_date"
                + ")) order by shift_start_date asc";

            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        using (OracleDataAdapter adapter = new OracleDataAdapter(cmd))
                        {
                            conn.Open();
                            adapter.Fill(dsShift);
                        }
                    }
                }
            }
            catch
            {
            }
            return dsShift;
        }

        // Check if employee has weekly off on date
        public bool IsEmplolyeeHasWeeklyOffForTheSelectedDate(string weekEndNumbers, string datenumber)
        {
            if (datenumber != "")
            {
                return weekEndNumbers.Contains(datenumber);
            }
            return false;
        }

        // Check if date is working day
        public bool isWorkingDay(DateTime vWorkingDayDate, String City)
        {
            try
            {
                DataTable WorkingDayTable = new DataTable();
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand("SELECT to_char(thl.WORKINGDAY_DATE,'dd Mon yyyy') as WORKINGDAY_DATE, thl.WORKINGDAY_NAME, thl.WORKINGDAY_DESC,thl.City FROM TS2_WORKINGDAY_LIST thl WHERE EXTRACT (YEAR FROM SYSDATE) <= EXTRACT (YEAR FROM WORKINGDAY_DATE) ORDER BY thl.WORKINGDAY_DATE ASC", conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        conn.Open();
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            WorkingDayTable.Load(reader);
                        }
                    }
                }

                String strFilter = "WORKINGDAY_DATE = '" + vWorkingDayDate.ToString("dd MMM yyyy") + "'and City = '" + City + "'";
                DataRow[] drArr = WorkingDayTable.Select(strFilter);

                if (drArr != null && drArr.Length == 1)
                    return true;
            }
            catch
            {
            }
            return false;
        }

        // Check if date is holiday
        public bool isHoliday(DateTime vHolidayDate, string city)
        {
            try
            {
                DataTable holidayTable = new DataTable();

                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand("SELECT to_char(thl.HOLIDAY_DATE,'dd Mon yyyy') as HOLIDAY_DATE, thl.HOLIDAY_NAME, thl.HOLIDAY_DESC,thl.City FROM TS2_HOLIDAY_LIST thl WHERE EXTRACT (YEAR FROM SYSDATE) -1 <= EXTRACT (YEAR FROM HOLIDAY_DATE) ORDER BY thl.HOLIDAY_DATE ASC", conn))
                    {
                        cmd.CommandType = CommandType.Text;

                        conn.Open();
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            holidayTable.Load(reader);
                        }
                    }
                }

                String strFilter = "HOLIDAY_DATE = '" + vHolidayDate.ToString("dd MMM yyyy") + "' and City = '" + city + "'";
                DataRow[] drArr = holidayTable.Select(strFilter);

                if (drArr != null && drArr.Length == 1)
                    return true;
            }
            catch
            {
            }
            return false;
        }

        // Get working days between two dates
        public int GetWorkingDaysBetween(DateTime dtFirst, DateTime dtSecond, int iEmployeeId, int leaveIncludesHoliday = 0)
        {
            try
            {
                DateTime dt = dtFirst;
                DateTime dtLast;
                int iDays = 0;
                int iWeekend = 71;

                bool IsAShiftEmployee = false;
                IsAShiftEmployee = IsShiftEmployeeDuringFirstAndSecondDate(iEmployeeId, dtFirst, dtSecond);
                string workingLocation = GetEmployeeWorkingLocation(iEmployeeId);

                if (IsAShiftEmployee)
                {
                    DataSet dsWeekend = new DataSet();
                    DataRow dRow;
                    dsWeekend = GetWeekendForShiftEmployees(iEmployeeId, dtFirst, dtSecond);

                    if (dsWeekend.Tables[0].Rows.Count == 0)
                    {
                        do
                        {
                            if (leaveIncludesHoliday == 1)
                            {
                                if ((dt == dtSecond) && ((IsEmplolyeeHasWeeklyOffForTheSelectedDate(Convert.ToString(iWeekend), Convert.ToString(Convert.ToInt32(dt.DayOfWeek) + 1))) && !isWorkingDay(dtSecond, workingLocation)))
                                {
                                    dt = dt.AddDays(1);
                                }
                                else if ((dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday) && !isWorkingDay(dt, workingLocation))
                                {
                                    dt = dt.AddDays(1);
                                    continue;
                                }
                                else
                                {
                                    dt = dt.AddDays(1);
                                    iDays++;
                                }
                            }
                            else
                            {
                                if ((dt == dtSecond) && (isHoliday(dtSecond, workingLocation) || ((IsEmplolyeeHasWeeklyOffForTheSelectedDate(Convert.ToString(iWeekend), Convert.ToString(Convert.ToInt32(dt.DayOfWeek) + 1))) && !isWorkingDay(dtSecond, workingLocation))))
                                {
                                    dt = dt.AddDays(1);
                                }
                                else if (isHoliday(dt, workingLocation) || ((dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday) && !isWorkingDay(dt, workingLocation)))
                                {
                                    dt = dt.AddDays(1);
                                    continue;
                                }
                                else
                                {
                                    dt = dt.AddDays(1);
                                    iDays++;
                                }
                            }
                        } while (dt <= dtSecond);
                    }

                    for (int iCount = 0; iCount < dsWeekend.Tables[0].Rows.Count; iCount++)
                    {
                        dRow = dsWeekend.Tables[0].Rows[iCount];
                        iWeekend = Convert.ToInt32(dRow["WEEKEND"]);
                        if (dsWeekend.Tables[0].Rows.Count > 1)
                        {
                            if (dtFirst >= Convert.ToDateTime(dRow["shift_start_date"]) && dtFirst <= Convert.ToDateTime(dRow["shift_end_date"]))
                            {
                                dt = dtFirst;
                            }
                            else
                            {
                                dt = Convert.ToDateTime(dRow["shift_start_date"]);
                            }

                            if (dtSecond >= Convert.ToDateTime(dRow["shift_start_date"]) && dtSecond <= Convert.ToDateTime(dRow["shift_end_date"]))
                            {
                                dtLast = dtSecond;
                            }
                            else
                            {
                                dtLast = Convert.ToDateTime(dRow["shift_end_date"]);
                            }

                            if ((iCount == (dsWeekend.Tables[0].Rows.Count - 1)) && dtSecond >= Convert.ToDateTime(dRow["shift_end_date"]))
                            {
                                dtLast = dtSecond;
                            }
                        }
                        else
                        {
                            dt = dtFirst;
                            dtLast = dtSecond;
                        }

                        do
                        {
                            if (leaveIncludesHoliday == 1)
                            {
                                if ((dt == dtLast) && ((IsEmplolyeeHasWeeklyOffForTheSelectedDate(Convert.ToString(iWeekend), Convert.ToString(Convert.ToInt32(dt.DayOfWeek) + 1))) && !isWorkingDay(dtLast, workingLocation)))
                                {
                                    dt = dt.AddDays(1);
                                    iDays++;
                                }
                                else if ((IsEmplolyeeHasWeeklyOffForTheSelectedDate(Convert.ToString(iWeekend), Convert.ToString(Convert.ToInt32(dt.DayOfWeek) + 1))) && !isWorkingDay(dt, workingLocation))
                                {
                                    dt = dt.AddDays(1);
                                    iDays++;
                                    continue;
                                }
                                else
                                {
                                    dt = dt.AddDays(1);
                                    iDays++;
                                }
                            }
                            else
                            {
                                if ((dt == dtLast) && (isHoliday(dtLast, workingLocation) || ((IsEmplolyeeHasWeeklyOffForTheSelectedDate(Convert.ToString(iWeekend), Convert.ToString(Convert.ToInt32(dt.DayOfWeek) + 1))) && !isWorkingDay(dtLast, workingLocation))))
                                {
                                    dt = dt.AddDays(1);
                                }
                                else if (isHoliday(dt, workingLocation) || ((IsEmplolyeeHasWeeklyOffForTheSelectedDate(Convert.ToString(iWeekend), Convert.ToString(Convert.ToInt32(dt.DayOfWeek) + 1))) && !isWorkingDay(dt, workingLocation)))
                                {
                                    dt = dt.AddDays(1);
                                    continue;
                                }
                                else
                                {
                                    dt = dt.AddDays(1);
                                    iDays++;
                                }
                            }
                        } while (dt <= dtLast);
                    }
                }
                else
                {
                    do
                    {
                        if (leaveIncludesHoliday == 1)
                        {
                            if ((dt == dtSecond) && ((IsEmplolyeeHasWeeklyOffForTheSelectedDate(Convert.ToString(iWeekend), Convert.ToString(Convert.ToInt32(dt.DayOfWeek) + 1))) && !isWorkingDay(dtSecond, workingLocation)))
                            {
                                dt = dt.AddDays(1);
                                iDays++;
                            }
                            else if ((IsEmplolyeeHasWeeklyOffForTheSelectedDate(Convert.ToString(iWeekend), Convert.ToString(Convert.ToInt32(dt.DayOfWeek) + 1))) && !isWorkingDay(dt, workingLocation))
                            {
                                dt = dt.AddDays(1);
                                iDays++;
                                continue;
                            }
                            else
                            {
                                dt = dt.AddDays(1);
                                iDays++;
                            }
                        }
                        else
                        {
                            if ((dt == dtSecond) && (isHoliday(dtSecond, workingLocation) || ((IsEmplolyeeHasWeeklyOffForTheSelectedDate(Convert.ToString(iWeekend), Convert.ToString(Convert.ToInt32(dt.DayOfWeek) + 1))) && !isWorkingDay(dtSecond, workingLocation))))
                            {
                                dt = dt.AddDays(1);
                            }
                            else if (isHoliday(dt, workingLocation) || ((IsEmplolyeeHasWeeklyOffForTheSelectedDate(Convert.ToString(iWeekend), Convert.ToString(Convert.ToInt32(dt.DayOfWeek) + 1))) && !isWorkingDay(dt, workingLocation)))
                            {
                                dt = dt.AddDays(1);
                                continue;
                            }
                            else
                            {
                                dt = dt.AddDays(1);
                                iDays++;
                            }
                        }
                    } while (dt <= dtSecond);
                }

                return iDays;
            }
            catch
            {
                return 0;
            }
        }

        // Check if employee is shift during period
        public bool IsShiftEmployeeDuringFirstAndSecondDate(int empId, DateTime dtFirst, DateTime dtSecond)
        {
            string sql = "select count(*) from RM_SHIFT_EMPLOYEES WHERE STATUS <> 'CANCELLED' AND emp_id = " + empId + "and "
                + " (shift_start_date between to_date('"
                + dtFirst.ToShortDateString() + "','mm/dd/yyyy')" + "and to_date('"
                + dtSecond.ToShortDateString() + "','mm/dd/yyyy')" + " OR "
                + "shift_start_date between to_date('"
                + dtFirst.ToShortDateString() + "','mm/dd/yyyy')" + "and to_date('"
                + dtSecond.ToShortDateString() + "','mm/dd/yyyy') OR "
                + "(to_date('" + dtFirst.ToShortDateString() + "','mm/dd/yyyy')"
                + "between shift_start_date and shift_end_date AND "
                + "to_date('" + dtSecond.ToShortDateString() + "','mm/dd/yyyy')"
                + "between shift_start_date and shift_end_date"
                + "))";

            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        conn.Open();
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Employee Leave Balance and Validation

        // Get employee working location
        public string GetEmployeeWorkingLocation(int emp_Id)
        {
            string workingLocation = String.Empty;
            try
            {
                string query;
                if (IsEmployeeELManuallyUpdated(emp_Id))
                {
                    query = @"select distinct aa.city from TS2_LOC_DESC aa,TS2_ENCASHABLE_MANUAL_UPDATE ab where ab.location = aa.PS_CITY and ab.emp_id = " + emp_Id;
                }
                else
                {
                    query = @"select distinct aa.city from TS2_LOC_DESC aa,ts2_employee_details ab where ab.location = aa.PS_CITY and ab.emp_id = " + emp_Id;
                }

                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        conn.Open();
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            workingLocation = result.ToString();
                        }
                    }
                }
            }
            catch
            {
            }
            return workingLocation;
        }

        // Check if employee EL manually updated
        public bool IsEmployeeELManuallyUpdated(int empId)
        {
            try
            {
                string sql = "SELECT count(*) FROM TS2_ENCASHABLE_MANUAL_UPDATE WHERE emp_id = " + empId + "";

                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        conn.Open();
                        var result = cmd.ExecuteScalar();
                        return Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // Get leaves of employee
        public DataSet GetLeavesOfEmployee(int empId)
        {
            DataSet ds = new DataSet();
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand("RM_LEAVE_PKG.GetLeavesOfEmployee", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("empid", OracleDbType.Int32).Value = empId;
                        cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        using (OracleDataAdapter adapter = new OracleDataAdapter(cmd))
                        {
                            conn.Open();
                            adapter.Fill(ds);
                        }
                    }
                }
            }
            catch
            {
            }
            return ds;
        }

        // Get attendance of employee
        public DataSet GetAttendanceOfEmployee(int empId)
        {
            DataSet ds = new DataSet();
            string sql = "SELECT ATTENDANCE_DETAILS_ID, START_DATE, END_DATE, ATTENDANCE_TYPE_ID FROM RM_ATTENDANCE_DETAILS WHERE EMP_ID = :empId AND (STATUS = 'Pending' OR STATUS = 'Approved' OR STATUS='ApprovedByTl') order by attendance_details_id desc";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                try
                {
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Add("empId", OracleDbType.Int32).Value = empId;
                        using (OracleDataAdapter adapter = new OracleDataAdapter(cmd))
                        {
                            conn.Open();
                            adapter.Fill(ds);
                        }
                    }
                }
                catch
                {
                }
            }
            return ds;
        }


        // Get casual leave balance for employee
        public async Task<CasualLeaveBalance> GetCasualLeaveBalanceAsync(int empId)
        {
            try
            {
                using (OracleConnection con = new OracleConnection(_connectionString))
                {
                    await con.OpenAsync();

                    var employeeInfo = await GetEmployeeTypeInfo(empId);

                    if (employeeInfo.EmployeeType == "CWR" ||
                        employeeInfo.EmployeeType == "INT" ||
                        employeeInfo.JobTitle?.ToUpper().Contains("INTERN") == true ||
                        employeeInfo.IsDirectContractor)
                    {
                        string query = @"
            SELECT COALESCE(SUM(PETO_TAKEN_FROM_CURRENT), 0) as LEAVES_TAKEN
            FROM TS2_EMP_LEAVE_DETAILS
            WHERE EMP_ID = :empId
              AND LEAVE_CAT_DESC_ID = 26
              AND STATUS NOT IN ('Cancelled', 'Rejected')
              AND TO_CHAR(LEAVE_START_DATE, 'MM-YYYY') = TO_CHAR(SYSDATE, 'MM-YYYY')";

                        using (OracleCommand cmd = new OracleCommand(query, con))
                        {
                            cmd.Parameters.Add(new OracleParameter("empId", OracleDbType.Int32) { Value = empId });

                            var result = await cmd.ExecuteScalarAsync();
                            double leavesTaken = 0;
                            if (result != null && result != DBNull.Value)
                            {
                                leavesTaken = Convert.ToDouble(result);
                            }
                            return new CasualLeaveBalance
                            {
                                LeaveBalance = 1 - leavesTaken
                            };
                        }
                    }

                    return new CasualLeaveBalance
                    {
                        LeaveBalance = 0
                    };
                }
            }
            catch
            {
                return new CasualLeaveBalance
                {
                    LeaveBalance = 0
                };
            }
        }

        // How many casual leaves taken in period
        public async Task<double> GetCasualLeavesTakenInPeriod(int empId, DateTime startDate, DateTime endDate)
        {
            double leavesTaken = 0;
            string query = @"
SELECT COALESCE(SUM(PETO_TAKEN_FROM_CURRENT), 0) as LEAVES_TAKEN
FROM ts2_emp_leave_details
WHERE emp_id = :empId
  AND leave_cat_desc_id = 26
  AND status NOT IN ('Cancelled', 'Rejected')
  AND leave_start_date <= :endDate
  AND leave_end_date >= :startDate";

            using (OracleConnection con = new OracleConnection(_connectionString))
            {
                try
                {
                    await con.OpenAsync();
                    using (OracleCommand cmd = new OracleCommand(query, con))
                    {
                        cmd.Parameters.Add(new OracleParameter("empId", empId));
                        cmd.Parameters.Add(new OracleParameter("startDate", startDate));
                        cmd.Parameters.Add(new OracleParameter("endDate", endDate));

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            leavesTaken = Convert.ToDouble(result);
                        }
                    }
                }
                catch
                {
                }
            }
            return leavesTaken;
        }

        // Calculate direct contractor leave balance
        private async Task<CasualLeaveBalance> CalculateDirectContractorLeaveBalance(int empId, DateTime joiningDate)
        {
            var balance = new CasualLeaveBalance();
            DateTime currentDate = DateTime.Now;
            int currentYear = currentDate.Year;

            bool isFirstHalf = currentDate.Month <= 6;
            DateTime halfYearStart = isFirstHalf ?
                new DateTime(currentYear, 1, 1) :
                new DateTime(currentYear, 7, 1);
            DateTime halfYearEnd = isFirstHalf ?
                new DateTime(currentYear, 6, 30) :
                new DateTime(currentYear, 12, 31);

            double proRataFactor = 1.0;
            int totalDaysInHalf = (int)(halfYearEnd - halfYearStart).TotalDays + 1;

            if (joiningDate > halfYearStart && joiningDate <= halfYearEnd)
            {
                int daysInPeriod = (int)(halfYearEnd - joiningDate).TotalDays + 1;
                proRataFactor = (double)daysInPeriod / totalDaysInHalf;
            }
            else if (joiningDate > halfYearEnd)
            {
                proRataFactor = 0;
            }

            double entitledDays = 9.0 * proRataFactor;

            double leavesTaken = await GetCasualLeavesTakenInPeriod(empId, halfYearStart, currentDate);

            balance.LeaveBalance = Math.Max(0, entitledDays - leavesTaken);

            return balance;
        }

        // Check for overlapping leave applications
        public bool IsLeaveOverLappingManageAttendance(int emp_id, DateTime date_tocompare_start, DateTime date_tocompare_end)
        {
            try
            {
                DataSet ds = GetLeavesOfEmployee(emp_id);

                DateTime currentDate = DateTime.Now;
                DateTime dateThreshold = currentDate.AddDays(-90);
                var filteredRows = ds.Tables[0].AsEnumerable()
                   .Where(row => row.Field<DateTime>("Leave_Start_Date") >= dateThreshold);

                DataTable tblFiltered;
                if (filteredRows.Any())
                {
                    tblFiltered = filteredRows.CopyToDataTable();
                }
                else
                {
                    tblFiltered = ds.Tables[0].Clone();
                }

                if (tblFiltered.Rows.Count > 0)
                {
                    foreach (DataRow dr in tblFiltered.Rows)
                    {
                        DateTime dt = date_tocompare_start;
                        while (dt <= date_tocompare_end)
                        {
                            if ((dt >= DateTime.Parse(dr["LEAVE_START_DATE"].ToString())) && (dt <= DateTime.Parse(dr["LEAVE_END_DATE"].ToString())))
                                return true;
                            else
                                dt = dt.AddDays(1);
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // Check for adjacent sick leave applications
        public async Task<bool> HasAdjacentSickLeave(int empId, DateTime leaveDate)
        {
            bool hasAdjacentLeave = false;

            string query = @"
SELECT COUNT(*) 
FROM TS2_EMP_LEAVE_DETAILS
WHERE EMP_ID = :empId
  AND LEAVE_CAT_DESC_ID IN (1, 26)
  AND STATUS IN ('Pending', 'Approved')
  AND (
      (TRUNC(:leaveDate) - 1 BETWEEN TRUNC(LEAVE_START_DATE) AND TRUNC(LEAVE_END_DATE))
      OR
      (TRUNC(:leaveDate) + 1 BETWEEN TRUNC(LEAVE_START_DATE) AND TRUNC(LEAVE_END_DATE))
      OR (TRUNC(LEAVE_START_DATE) = TRUNC(:leaveDate) + 1)
      OR (TRUNC(LEAVE_END_DATE) = TRUNC(:leaveDate) - 1)
  )";

            using (OracleConnection con = new OracleConnection(_connectionString))
            {
                try
                {
                    await con.OpenAsync();
                    using (OracleCommand cmd = new OracleCommand(query, con))
                    {
                        cmd.Parameters.Add(new OracleParameter("empId", empId));
                        cmd.Parameters.Add(new OracleParameter("leaveDate", leaveDate));

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            int count = Convert.ToInt32(result);
                            hasAdjacentLeave = count > 0;
                        }
                    }
                }
                catch
                {
                }
            }

            return hasAdjacentLeave;
        }

        // Calculate working days for leave validation
        public async Task<double> CalculateWorkingDays(int empId, DateTime startDate, DateTime endDate, bool includeHolidays)
        {
            double workingDays = GetWorkingDaysBetween(startDate, endDate, empId, includeHolidays ? 1 : 0);
            return workingDays;
        }

        #endregion

        #region Missing Methods for Leave Strategies
        public async Task<EmployeeTypeInfo> GetEmployeeTypeInfo(int empId)
        {
            var employeeInfo = new EmployeeTypeInfo();
            string query = @"
        SELECT 
            PS_EMP_TYPE, 
            DIRECT_CONTRACTOR,
            JOINING_DATE,
            EMP_NAME,
            JOB_TITLE_FULL
        FROM 
            TS2_EMPLOYEE_DETAILS 
        WHERE 
            EMP_ID = :empId";

            using (OracleConnection con = new OracleConnection(_connectionString))
            {
                try
                {
                    await con.OpenAsync();
                    using (OracleCommand cmd = new OracleCommand(query, con))
                    {
                        cmd.Parameters.Add(new OracleParameter("empId", empId));
                        using (OracleDataReader reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string empType = reader["PS_EMP_TYPE"]?.ToString() ?? "";
                                string directContractor = reader["DIRECT_CONTRACTOR"]?.ToString() ?? "";
                                string jobTitle = reader.IsDBNull(reader.GetOrdinal("JOB_TITLE_FULL")) ? string.Empty : reader["JOB_TITLE_FULL"].ToString();

                                employeeInfo.EmployeeType = empType;
                                employeeInfo.JobTitle = jobTitle;
                                employeeInfo.IsDirectContractor = directContractor.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

                                if (!reader.IsDBNull(reader.GetOrdinal("JOINING_DATE")))
                                {
                                    employeeInfo.JoiningDate = reader.GetDateTime(reader.GetOrdinal("JOINING_DATE"));
                                }
                                else
                                {
                                    employeeInfo.JoiningDate = new DateTime(1900, 1, 1);
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("EMP_NAME")))
                                {
                                    employeeInfo.EmployeeName = reader.GetString(reader.GetOrdinal("EMP_NAME"));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
            return employeeInfo;
        }

        public async Task<double> GetLeavesAlreadyTakenInMonth(int empId, DateTime leaveDate)
        {
            double takenDays = 0;

            string query = @"
SELECT LEAVE_START_DATE, LEAVE_END_DATE
FROM TS2_EMP_LEAVE_DETAILS
WHERE EMP_ID = :empId
  AND LEAVE_CAT_DESC_ID = 26  -- Casual Leave
  AND STATUS NOT IN ('Cancelled', 'Rejected')
  AND TO_CHAR(LEAVE_START_DATE, 'MM-YYYY') = TO_CHAR(:leaveDate, 'MM-YYYY')";

            using (OracleConnection con = new OracleConnection(_connectionString))
            {
                try
                {
                    await con.OpenAsync();
                    using (OracleCommand cmd = new OracleCommand(query, con))
                    {
                        cmd.Parameters.Add(new OracleParameter("empId", OracleDbType.Int32) { Value = empId });
                        cmd.Parameters.Add(new OracleParameter("leaveDate", OracleDbType.Date) { Value = leaveDate });

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var startDate = reader.GetDateTime("LEAVE_START_DATE");
                                var endDate = reader.GetDateTime("LEAVE_END_DATE");

                                // Calculate working days for each leave record
                                double workingDays = await CalculateWorkingDays(empId, startDate, endDate, false);
                                takenDays += workingDays;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }

            return takenDays;
        }
        public async Task<double> GetLeavesAlreadyTakenInYear(int empId, int year, int leaveTypeId)
        {
            double takenDays = 0;

            string query = @"
        SELECT COALESCE(SUM(PETO_TAKEN_FROM_CURRENT), 0) as LEAVES_TAKEN
        FROM TS2_EMP_LEAVE_DETAILS
        WHERE EMP_ID = :empId
          AND LEAVE_CAT_DESC_ID = :leaveTypeId
          AND STATUS NOT IN ('Cancelled', 'Rejected')
          AND EXTRACT(YEAR FROM LEAVE_START_DATE) = :year";

            using (OracleConnection con = new OracleConnection(_connectionString))
            {
                try
                {
                    await con.OpenAsync();
                    using (OracleCommand cmd = new OracleCommand(query, con))
                    {
                        cmd.Parameters.Add(new OracleParameter("empId", OracleDbType.Int32) { Value = empId });
                        cmd.Parameters.Add(new OracleParameter("leaveTypeId", OracleDbType.Int32) { Value = leaveTypeId });
                        cmd.Parameters.Add(new OracleParameter("year", OracleDbType.Int32) { Value = year });

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            takenDays = Convert.ToDouble(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }

            return takenDays;
        }

        public async Task<bool> HasMedicalCertificateUploaded(int empId, DateTime startDate, DateTime endDate)
        {            
            string query = @"
                SELECT COUNT(*) 
                FROM TS2_EMPLOYEE_DOCUMENTS 
                WHERE EMP_ID = :empId 
                  AND DOCUMENT_TYPE = 'MEDICAL_CERTIFICATE'
                  AND DOCUMENT_DATE BETWEEN :startDate AND :endDate
                  AND STATUS = 'APPROVED'";
           
            return await Task.FromResult(true); 
        }
        public async Task<bool> IsWeekendOrHolidayAsync(DateTime date, int employeeId)
        {
            return await Task.FromResult(
                date.DayOfWeek == DayOfWeek.Saturday ||
                date.DayOfWeek == DayOfWeek.Sunday ||
                isHoliday(date, GetEmployeeWorkingLocation(employeeId))
            );
        }
        #endregion

        #region Direct Contractor Leave Methods (SQL Query Based)

        public async Task<double[]> GetMaximumPermissibleSickLeaves_ForDirectContractor(int empId)
        {
            double[] leaveDetails = new double[3] { 0, 0, 12 }; // Default values

            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if employee is direct contractor and calculate earned leaves
                    using (var command = new OracleCommand())
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.Text;
                        command.CommandText = @"
                    SELECT 
                        CASE 
                            WHEN UPPER(ed.DIRECT_CONTRACTOR) IN ('TRUE', 'YES', 'Y', '1') THEN
                                LEAST(FLOOR(MONTHS_BETWEEN(SYSDATE, ed.JOINING_DATE)) * 1.0, 12)
                            ELSE 0
                        END AS EARNED_TILL_DATE,
                        NVL(SUM(CASE 
                            WHEN ld.STATUS IN ('Approved', 'Pending') 
                            AND ld.LEAVE_CAT_DESC_ID IN (1, 25) 
                            AND EXTRACT(YEAR FROM ld.LEAVE_START_DATE) = EXTRACT(YEAR FROM SYSDATE)
                            THEN ld.PETO_TAKEN_FROM_CURRENT 
                            ELSE 0 
                        END), 0) AS AVAILED_TOTAL,
                        12 AS MAX_PERMISSIBLE,
                        ed.DIRECT_CONTRACTOR,
                        ed.JOINING_DATE,
                        MONTHS_BETWEEN(SYSDATE, ed.JOINING_DATE) as MONTHS_WORKED
                    FROM TS2_EMPLOYEE_DETAILS ed
                    LEFT JOIN TS2_EMP_LEAVE_DETAILS ld ON ed.EMP_ID = ld.EMP_ID
                    WHERE ed.EMP_ID = :P_EMP_ID
                    GROUP BY ed.EMP_ID, ed.JOINING_DATE, ed.DIRECT_CONTRACTOR";

                        command.Parameters.Add("P_EMP_ID", OracleDbType.Int32).Value = empId;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                leaveDetails[0] = reader.IsDBNull(0) ? 0 : Convert.ToDouble(reader.GetValue(0));
                                leaveDetails[1] = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetValue(1));
                                leaveDetails[2] = reader.IsDBNull(2) ? 18 : Convert.ToDouble(reader.GetValue(2));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Return default values
                leaveDetails[0] = 0;  // Earned
                leaveDetails[1] = 0;  // Availed
                leaveDetails[2] = 12; // Max permissible
            }

            return leaveDetails;
        }
        public async Task<double[]> GetMaximumPermissibleCasualLeaves_ForDirectContractor(int empId)
        {
            Double[] leavedetails = new Double[3] { 0, 0, 18 }; // [earned, availed, max]

            try
            {
                var employeeInfo = await GetEmployeeTypeInfo(empId);
                DateTime currentDate = DateTime.Today;
                DateTime dFirst = new DateTime(currentDate.Year, 1, 1);
                DateTime dLast = new DateTime(currentDate.Year, 12, 31);
                DateTime dHalfYear = new DateTime(currentDate.Year, 7, 1);
                DateTime empJoiningDate = employeeInfo.JoiningDate;

                double casualLeavesTaken_Approved = 0;
                double casualLeavesTaken_Pending = 0;
                double casualLeavesEarned = 0;
                double MonthlyCasualLeaveCredited = 1.5; 
                Double total_Casual_leave_alloted_Inyear = 0.0;

                // For employees who joined before this year
                if (empJoiningDate < dFirst)
                {
                    casualLeavesEarned = MonthlyCasualLeaveCredited * currentDate.Month;

                    if (currentDate < dHalfYear)
                        total_Casual_leave_alloted_Inyear = Math.Round((MonthlyCasualLeaveCredited * 6), 2);
                    else
                        total_Casual_leave_alloted_Inyear = Math.Round((MonthlyCasualLeaveCredited * 12), 2);

                    casualLeavesTaken_Approved = await GetCasualLeavesTakenFromStartOfYear_Approved(empId, dFirst, dLast);
                    casualLeavesTaken_Pending = await GetCasualLeavesTakenFromStartOfYear_Pending(empId, dFirst, dLast);
                }
                else
                {
                    double casualLeaveEarnedInJoiningMonth = 0;
                    double casualLeaveEarnedInJoiningMonthActual = 0;
                    double daysInPeriod = 0;
                    double _noOfDays = 0;
                    double casualLeaveEarnedPerDay = 0;

                    if (currentDate.Year == empJoiningDate.Year && currentDate.Month == empJoiningDate.Month)
                    {
                        _noOfDays = (currentDate - empJoiningDate).TotalDays;
                        daysInPeriod = 30;
                        casualLeaveEarnedPerDay = Math.Round(MonthlyCasualLeaveCredited / daysInPeriod, 3);
                        casualLeaveEarnedInJoiningMonthActual = Math.Round(casualLeaveEarnedPerDay * _noOfDays, 2);
                    }
                    else
                    {
                        double daysInMonth = (DateTime.DaysInMonth(empJoiningDate.Year, empJoiningDate.Month));
                        casualLeaveEarnedPerDay = Math.Round(MonthlyCasualLeaveCredited / daysInMonth, 3);
                        casualLeaveEarnedInJoiningMonthActual = Math.Round(casualLeaveEarnedPerDay * (daysInMonth - empJoiningDate.Day), 2);
                    }

                    casualLeaveEarnedInJoiningMonth = casualLeaveEarnedInJoiningMonthActual < 0.25 ? 0 :
                        (casualLeaveEarnedInJoiningMonthActual >= 0.25 && casualLeaveEarnedInJoiningMonthActual <= 0.5) ? 0.5 :
                        (casualLeaveEarnedInJoiningMonthActual >= 0.51 && casualLeaveEarnedInJoiningMonthActual <= 0.75) ? 0.75 :
                        (casualLeaveEarnedInJoiningMonthActual >= 0.76 && casualLeaveEarnedInJoiningMonthActual <= 1) ? 1 :
                        (casualLeaveEarnedInJoiningMonthActual > 1 && casualLeaveEarnedInJoiningMonthActual <= 1.25) ? 1.25 :
                        (casualLeaveEarnedInJoiningMonthActual > 1.25 && casualLeaveEarnedInJoiningMonthActual <= 1.5) ? 1.5 : 1.5;

                    if (currentDate.Year == empJoiningDate.Year && currentDate.Month == empJoiningDate.Month)
                        casualLeavesEarned = casualLeaveEarnedInJoiningMonth;
                    else
                        casualLeavesEarned = Math.Round(((currentDate.Month - empJoiningDate.Month) * MonthlyCasualLeaveCredited) + casualLeaveEarnedInJoiningMonth, 2);

                    if (empJoiningDate < dHalfYear)
                    {
                        if (currentDate < dHalfYear)
                            total_Casual_leave_alloted_Inyear = Math.Round((((dHalfYear.Month - 1) - empJoiningDate.Month) * MonthlyCasualLeaveCredited) + casualLeaveEarnedInJoiningMonth, 2);
                        else
                            total_Casual_leave_alloted_Inyear = Math.Round((((dHalfYear.Month - 1) - empJoiningDate.Month) * MonthlyCasualLeaveCredited) + casualLeaveEarnedInJoiningMonth + Math.Round((MonthlyCasualLeaveCredited * 6), 2), 2);
                    }
                    else if (empJoiningDate > dHalfYear)
                    {
                        total_Casual_leave_alloted_Inyear = Math.Round(((dLast.Month - empJoiningDate.Month) * MonthlyCasualLeaveCredited) + casualLeaveEarnedInJoiningMonth, 2);
                    }
                    else if (empJoiningDate.Date == dHalfYear.Date)
                    {
                        total_Casual_leave_alloted_Inyear = Math.Round(((dLast.Month - (empJoiningDate.Month - 1)) * MonthlyCasualLeaveCredited), 2);
                    }

                    casualLeavesTaken_Approved = await GetCasualLeavesTakenFromStartOfYear_Approved(empId, empJoiningDate, dLast);
                    casualLeavesTaken_Pending = await GetCasualLeavesTakenFromStartOfYear_Pending(empId, empJoiningDate, dLast);
                }

                double casualLeavesTaken_Total = casualLeavesTaken_Approved + casualLeavesTaken_Pending;

                leavedetails[0] = Math.Round(casualLeavesEarned, 2); // Earned
                leavedetails[1] = casualLeavesTaken_Total; // Availed
                leavedetails[2] = Math.Round(total_Casual_leave_alloted_Inyear, 2); // Max permissible
            }
            catch (Exception ex)
            {
            }

            return leavedetails;
        }

        // Supporting methods for casual leave calculations
        public async Task<double> GetCasualLeavesTakenFromStartOfYear_Approved(int empId, DateTime startDate, DateTime endDate)
        {
            double leavesTaken = 0;
            string query = @"
        SELECT COALESCE(SUM(PETO_TAKEN_FROM_CURRENT), 0) as LEAVES_TAKEN
        FROM ts2_emp_leave_details
        WHERE emp_id = :empId
          AND leave_cat_desc_id = 26
          AND status = 'Approved'
          AND leave_start_date >= :startDate
          AND leave_end_date <= :endDate";

            using (OracleConnection con = new OracleConnection(_connectionString))
            {
                try
                {
                    await con.OpenAsync();
                    using (OracleCommand cmd = new OracleCommand(query, con))
                    {
                        cmd.Parameters.Add(new OracleParameter("empId", empId));
                        cmd.Parameters.Add(new OracleParameter("startDate", startDate));
                        cmd.Parameters.Add(new OracleParameter("endDate", endDate));

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            leavesTaken = Convert.ToDouble(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
            return leavesTaken;
        }

        public async Task<double> GetCasualLeavesTakenFromStartOfYear_Pending(int empId, DateTime startDate, DateTime endDate)
        {
            double leavesTaken = 0;
            string query = @"
        SELECT COALESCE(SUM(PETO_TAKEN_FROM_CURRENT), 0) as LEAVES_TAKEN
        FROM ts2_emp_leave_details
        WHERE emp_id = :empId
          AND leave_cat_desc_id = 26
          AND status = 'Pending'
          AND leave_start_date >= :startDate
          AND leave_end_date <= :endDate";

            using (OracleConnection con = new OracleConnection(_connectionString))
            {
                try
                {
                    await con.OpenAsync();
                    using (OracleCommand cmd = new OracleCommand(query, con))
                    {
                        cmd.Parameters.Add(new OracleParameter("empId", empId));
                        cmd.Parameters.Add(new OracleParameter("startDate", startDate));
                        cmd.Parameters.Add(new OracleParameter("endDate", endDate));

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            leavesTaken = Convert.ToDouble(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
            return leavesTaken;
        }

        public async Task<DateTime> GetEmployeeTransferDate(int empId)
        {
            DateTime transferDate = DateTime.MinValue;
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    // Query based on the legacy code's GetEmployeeTransferDate implementation
                    string query = @"
                SELECT TRANSFER_DATE
                FROM TS2_EMPLOYEE_DETAILS
                WHERE EMP_ID = :empId";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add("empId", OracleDbType.Int32).Value = empId;
                        await conn.OpenAsync();

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            transferDate = Convert.ToDateTime(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return transferDate;
        }
        public async Task<double> GetSickLeavesInExtendedPeriod(int empId, DateTime startDate, DateTime endDate)
        {
            double sickLeavesTaken = 0;

            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand())
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.Text;

                        // SQL query to get sick leaves in period (including pending)
                        command.CommandText = @"
                    SELECT NVL(SUM(ld.PETO_TAKEN_FROM_CURRENT), 0) AS SICK_LEAVES_TAKEN
                    FROM TS2_EMP_LEAVE_DETAILS ld
                    WHERE ld.EMP_ID = :P_EMP_ID
                        AND ld.STATUS IN ('Approved', 'Pending')
                        AND ld.LEAVE_CAT_DESC_ID IN (1, 25)
                        AND (
                            (ld.LEAVE_START_DATE >= :P_START_DATE AND ld.LEAVE_START_DATE <= :P_END_DATE) OR
                            (ld.LEAVE_END_DATE >= :P_START_DATE AND ld.LEAVE_END_DATE <= :P_END_DATE) OR
                            (ld.LEAVE_START_DATE <= :P_START_DATE AND ld.LEAVE_END_DATE >= :P_END_DATE)
                        )";

                        command.Parameters.Add("P_EMP_ID", OracleDbType.Int32).Value = empId;
                        command.Parameters.Add("P_START_DATE", OracleDbType.Date).Value = startDate;
                        command.Parameters.Add("P_END_DATE", OracleDbType.Date).Value = endDate;

                        var result = await command.ExecuteScalarAsync();
                        sickLeavesTaken = result != DBNull.Value ? Convert.ToDouble(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return sickLeavesTaken;
        }


        #endregion

        #region Helper Methods for Direct Contractor Validation
        public async Task<bool> IsDirectContractor(int empId)
        {
            try
            {
                using (var connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand())
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.Text;
                        command.CommandText = @"
                    SELECT DIRECT_CONTRACTOR 
                    FROM TS2_EMPLOYEE_DETAILS 
                    WHERE EMP_ID = :P_EMP_ID";

                        command.Parameters.Add("P_EMP_ID", OracleDbType.Int32).Value = empId;

                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            string directContractor = result.ToString().ToUpper().Trim();
                            bool isDirectContractor = directContractor == "TRUE" || directContractor == "YES" ||
                                                     directContractor == "Y" || directContractor == "1";

                            return isDirectContractor;
                        }
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<double> GetDirectContractorCasualLeaveBalance(int empId)
        {
            try
            {
                var casualLeaveDetails = await GetMaximumPermissibleCasualLeaves_ForDirectContractor(empId);
                double earned = casualLeaveDetails[0];
                double availed = casualLeaveDetails[1];
                double balance = Math.Max(0, earned - availed);
                return balance;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        #endregion
    }
}