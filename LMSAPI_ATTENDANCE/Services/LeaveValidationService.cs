using LMSAPI_ATTENDANCE.Model;
using LMSAPI_ATTENDANCE.Repository;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LMSAPI_ATTENDANCE.Services
{
    public class LeaveValidationService
    {
        private readonly MyDataRepository _repository;
        private readonly IConfiguration _configuration;

        public LeaveValidationService(MyDataRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
        }

        // PTO/sick leave policy validation
        public async Task<LeaveValidationResult> ValidateLegacyLeavePolicy(int empId, DateTime startDate, DateTime endDate, int leaveTypeId)
        {
            try
            {
                if (leaveTypeId == (int)LeaveType.PaidTimeOff || leaveTypeId == (int)LeaveType.CasualLeave)
                {
                    List<DateTime> periodDates = _repository.GetFirstAndLastDatesOfPeriod(empId, startDate, endDate);

                    double sickLeavesTaken = _repository.GetSickLeavesTakenFromCurrentYear(
                        empId,
                        periodDates[0].AddDays(-1),
                        periodDates[1].AddDays(1));

                    if (sickLeavesTaken > 0)
                    {
                        return LeaveValidationResult.Failure(
                            "According to the leave policy, combining PTO and sick leave is not permitted. " +
                            "Please submit HR ticket along with an approval email from your Manager for further review.");
                    }
                }

                return LeaveValidationResult.Success();
            }
            catch (Exception ex)
            {
                return LeaveValidationResult.Failure("Error validating leave policy. Please try again.");
            }
        }

        // Leave and attendance overlap validation
        public async Task<LeaveValidationResult> ValidateOverlaps(int empId, DateTime startDate, DateTime endDate, int leaveTypeId)
        {
            try
            {
                if (_repository.IsLeaveOverLappingManageAttendance(empId, startDate, endDate))
                {
                    return LeaveValidationResult.Failure(
                        "This leave application is overlapping with another leave application. " +
                        "Kindly contact your lead to cancel the conflicting application and try again.");
                }

                if (IsAttendanceOverlapping(empId, startDate, endDate))
                {
                    return LeaveValidationResult.Failure(
                        "This leave application is overlapping with an attendance application. " +
                        "Kindly contact your lead to reject the attendance application and try again.");
                }

                // Casual leave adjacent restriction check
                if (leaveTypeId == (int)LeaveType.CasualLeave)
                {
                    var adjacentCheckDays = _configuration.GetValue<int>("LeaveSettings:CasualLeave:AdjacentLeaveCheckDays", 1);
                    if (await HasAdjacentRestrictedLeave(empId, startDate, endDate, adjacentCheckDays))
                    {
                        return LeaveValidationResult.Failure(
                            "Casual leave cannot be clubbed with any other type of leave.");
                    }
                }

                return LeaveValidationResult.Success();
            }
            catch
            {
                return LeaveValidationResult.Failure("Error validating overlaps. Please try again.");
            }
        }

        public bool ValidateAppliedDate(DateTime leaveStartDate, int allowedWorkingDays = 6)
        {
            var today = DateTime.Today;

            if (leaveStartDate.Date > today)
                return true;  // Allow all future dates

            // Count working days between dates
            var workingDaysDifference = CountWorkingDays(leaveStartDate.Date, today);
            return workingDaysDifference <= allowedWorkingDays;
        }

        private int CountWorkingDays(DateTime startDate, DateTime endDate)
        {
            int workingDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    workingDays++;
            }
            return workingDays - 1; // Don't count the end date
        }

        // Attendance overlap check within 90-day window
        private bool IsAttendanceOverlapping(int empId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var ds = _repository.GetAttendanceOfEmployee(empId);

                DateTime dateThreshold = DateTime.Now.AddDays(-90);
                var filteredRows = ds.Tables[0].AsEnumerable()
                    .Where(row => row.Field<DateTime>("Start_Date") >= dateThreshold);

                DataTable tblFiltered = filteredRows.Any() ?
                    filteredRows.CopyToDataTable() :
                    ds.Tables[0].Clone();

                foreach (DataRow dr in tblFiltered.Rows)
                {
                    DateTime dt = startDate;
                    while (dt <= endDate)
                    {
                        int attendanceTypeId = Convert.ToInt32(dr["Attendance_Type_Id"]);

                        if (attendanceTypeId != (int)AttendanceType.Others)
                        {
                            DateTime attendanceStart = DateTime.Parse(dr["START_DATE"].ToString());
                            DateTime attendanceEnd = DateTime.Parse(dr["END_DATE"].ToString());

                            if (dt >= attendanceStart && dt <= attendanceEnd)
                            {
                                // Skip WFH and Onsite types
                                if (attendanceTypeId != (int)AttendanceType.WorkFromHome &&
                                    attendanceTypeId != (int)AttendanceType.Onsite)
                                {
                                    return true;
                                }
                            }
                        }
                        dt = dt.AddDays(1);
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // Adjacent leave type restriction check
        private async Task<bool> HasAdjacentRestrictedLeave(int empId, DateTime startDate, DateTime endDate, int checkDays)
        {
            try
            {
                for (DateTime checkDate = startDate; checkDate <= endDate; checkDate = checkDate.AddDays(1))
                {
                    if (await _repository.HasAdjacentSickLeave(empId, checkDate))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}