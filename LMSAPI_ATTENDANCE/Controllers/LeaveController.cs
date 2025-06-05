using LMSAPI_ATTENDANCE.Model;
using LMSAPI_ATTENDANCE.Repository;
using LMSAPI_ATTENDANCE.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LMSAPI.Controllers
{
    [Route("api/v1/")]
    [ApiController]
    public class LeaveController : ControllerBase
    {
        private readonly LeaveApplicationService _leaveService;
        private readonly MyDataRepository _repository;

        public LeaveController(LeaveApplicationService leaveService, MyDataRepository repository)
        {
            _leaveService = leaveService;
            _repository = repository;
        }

        [HttpPost("Attendance/ApplyAttendance")]
        public async Task<IActionResult> Post([FromBody] Attendance data)
        {
            // Check leave-attendance overlap
            if (!_repository.IsLeaveOverLappingManageAttendance(data.emp_id, data.start_date, data.end_date))
            {
                // Check existing attendance overlap
                if (!IsAttendanceOverLappingManageAttendance(data.emp_id, data.start_date, data.end_date))
                {
                    var result = await _repository.InsertAttendanceAsync(data);

                    if (result == "Attendance can be applied only for the working days")
                        return Ok(new ResponseModel { ID = "0", Message = result });

                    if (result != "Issue")
                        return Ok(new ResponseModel { ID = result, Message = "Attendance application submitted successfully!." });

                    return Ok(new ResponseModel { ID = "0", Message = "Attendance application not submitted! Please Reapply." });
                }
                return Ok(new ResponseModel { ID = "0", Message = "A previous application falls within the specified duration." });
            }
            return Ok(new ResponseModel { ID = "0", Message = "Attendance is overlapping with leave application" });
        }

        [HttpPost("Attendance/ApplyProxyAttendance")]
        public async Task<IActionResult> PostAttendance([FromBody] Attendance data)
        {
            // Check leave-attendance overlap
            if (!_repository.IsLeaveOverLappingManageAttendance(data.emp_id, data.start_date, data.end_date))
            {
                // Check existing attendance overlap
                if (!IsAttendanceOverLappingManageAttendance(data.emp_id, data.start_date, data.end_date))
                {
                    var result = await _repository.InsertAttendanceAsync(data);

                    if (result == "Attendance can be applied only for the working days")
                        return Ok(new ResponseModel { ID = "0", Message = result });

                    if (result != "Issue")
                        return Ok(new ResponseModel { ID = result, Message = "Attendance application submitted successfully!." });

                    return Ok(new ResponseModel { ID = "0", Message = "Attendance application not submitted! Please Reapply." });
                }
                return Ok(new ResponseModel { ID = "0", Message = "A previous application falls within the specified duration." });
            }
            return Ok(new ResponseModel { ID = "0", Message = "Attendance is overlapping with leave application" });
        }

        [HttpPost("Leave/ApplyLeave")]
        public async Task<IActionResult> Post([FromBody] Leave data)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new ResponseModel { ID = "0", Message = string.Join("; ", errors) });
                }

                // Validate date range
                if (data.end_date < data.start_date)
                    return BadRequest(new ResponseModel { ID = "0", Message = "End date cannot be before start date." });

                if (data.leave_type_id == 26) { 

                var result = await _leaveService.ApplyLeaveAsync(data); 
                    
                    return Ok(new ResponseModel
                        {
                            ID = result.Success ? result.LeaveId : "0",
                            Message = result.Message
                        });
                }
                else
                {

                    //var appliedDateLimit = _configuration.GetValue<int>("LeaveSettings:Common:AppliedDateLimitDays", 6);
                    if (!ValidateAppliedDate(data.start_date, 6))
                    {


                        var response = new ResponseModel
                        {
                            ID = "0",
                            Message = $"Leaves cannot be applied later than 6 working days. Please refer to India Leave policy for more details."
                        };

                        return Ok(response);

                    }

                    if (IsLeaveOverLappingManageAttendance(data.emp_id, data.start_date, data.end_date) == false)
                    {

                        var result = await _repository.InsertLeaveAsync(data);

                        if (result == "Leave can be applied only for the working days")
                        {

                            var response = new ResponseModel
                            {
                                ID = "0",
                                Message = result
                            };

                            return Ok(response);
                        }
                        else
                        {
                            var response = new ResponseModel
                            {
                                ID = result,
                                Message = "Leave application submitted successfully !."
                            };
                            return Ok(response);
                        }
                    }
                    else
                    {
                        var response = new ResponseModel
                        {
                            ID = "0",
                            Message = "Leave is overlapping with leave application"
                        };
                        return Ok(response);
                    }

                }

               
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel { ID = "0", Message = "An error occurred while processing your request: " + ex.Message });
            }
        }

        [HttpPost("Leave/ApplyProxyLeaves")]
        public async Task<IActionResult> PostLeave([FromBody] Leave data)
        {
            try
            {
                var result = await _leaveService.ApplyLeaveAsync(data);
                return Ok(new ResponseModel
                {
                    ID = result.Success ? result.LeaveId : "0",
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                return Ok(new ResponseModel { ID = "0", Message = "An error occurred while processing your request: " + ex.Message });
            }
        }

        // Validates overlap between leave and attendance periods
        public static bool IsLeaveAndAttendanceOverLappingManageAttendance(int emp_id, DateTime start, DateTime end, bool isLeave, bool Cog = false)
        {
            if (!isLeave)
            {
                // Check against existing leaves
                var ds = LMSAPI_ATTENDANCE.Repository.LeaveService.GetLeavesOfEmployee(emp_id);
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    DateTime dt = start;
                    while (dt <= end)
                    {
                        if (dt >= DateTime.Parse(dr["LEAVE_START_DATE"].ToString()) && dt <= DateTime.Parse(dr["LEAVE_END_DATE"].ToString()))
                            return true;
                        dt = dt.AddDays(1);
                    }
                }
                return false;
            }
            else
            {
                // Check against existing attendance
                var ds = LMSAPI_ATTENDANCE.Repository.LeaveService.GetAttendanceOfEmployee(emp_id);
                if (Cog)
                    ds.Tables[0].DefaultView.RowFilter = " ATTENDANCE_TYPE_ID = 9";
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    DateTime dt = start;
                    while (dt <= end)
                    {
                        if (Convert.ToInt32(dr["Attendance_Type_Id"]) != 5)
                        {
                            // Exclude WFH and Onsite attendance types
                            if (dt >= DateTime.Parse(dr["START_DATE"].ToString()) && dt <= DateTime.Parse(dr["END_DATE"].ToString()) && Convert.ToInt32(dr["Attendance_Type_Id"]) != (int)AttendanceType.WorkFromHome && Convert.ToInt32(dr["Attendance_Type_Id"]) != (int)AttendanceType.Onsite)
                                return true;
                            dt = dt.AddDays(1);
                        }
                        else
                            dt = dt.AddDays(1);
                    }
                }
                return false;
            }
        }

        // Checks for overlapping leave applications within 90-day window
        public static bool IsLeaveOverLappingManageAttendance(int emp_id, DateTime start, DateTime end)
        {
            var ds = LMSAPI_ATTENDANCE.Repository.LeaveService.GetLeavesOfEmployee(emp_id);

            // Filter to last 90 days for performance
            DateTime dateThreshold = DateTime.Now.AddDays(-90);
            var filteredRows = ds.Tables[0].AsEnumerable()
               .Where(row => row.Field<DateTime>("Leave_Start_Date") >= dateThreshold);

            DataTable tblFiltered = filteredRows.Any() ? filteredRows.CopyToDataTable() : ds.Tables[0].Clone();

            foreach (DataRow dr in tblFiltered.Rows)
            {
                DateTime dt = start;
                while (dt <= end)
                {
                    if (dt >= DateTime.Parse(dr["LEAVE_START_DATE"].ToString()) && dt <= DateTime.Parse(dr["LEAVE_END_DATE"].ToString()))
                        return true;
                    dt = dt.AddDays(1);
                }
            }
            return false;
        }

        // Checks for overlapping attendance records within 90-day window
        public static bool IsAttendanceOverLappingManageAttendance(int emp_id, DateTime start, DateTime end)
        {
            var ds = LMSAPI_ATTENDANCE.Repository.LeaveService.GetAttendanceOfEmployee(emp_id);

            // Filter to last 90 days for performance
            DateTime dateThreshold = DateTime.Now.AddDays(-90);
            var filteredRows = ds.Tables[0].AsEnumerable()
                .Where(row => row.Field<DateTime>("Start_Date") >= dateThreshold);
            DataTable tblFiltered = filteredRows.Any() ? filteredRows.CopyToDataTable() : ds.Tables[0].Clone();

            foreach (DataRow dr in tblFiltered.Rows)
            {
                DateTime dt = start;
                while (dt <= end)
                {
                    // Skip attendance type 5
                    if (Convert.ToInt32(dr["Attendance_Type_Id"]) != 5)
                    {
                        if (dt >= DateTime.Parse(dr["START_DATE"].ToString()) && dt <= DateTime.Parse(dr["END_DATE"].ToString()))
                            return true;
                        dt = dt.AddDays(1);
                    }
                    else
                        dt = dt.AddDays(1);
                }
            }
            return false;
        }

        public static bool ValidateAppliedDate(DateTime leaveStartDate, int allowedWorkingDays = 6)
        {
            var today = DateTime.Today;

            if (leaveStartDate.Date > today)
                return true;  // Allow all future dates

            // Count working days between dates
            var workingDaysDifference = CountWorkingDays(leaveStartDate.Date, today);
            return workingDaysDifference <= allowedWorkingDays;
        }

        private static int  CountWorkingDays(DateTime startDate, DateTime endDate)
        {
            int workingDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    workingDays++;
            }
            return workingDays - 1; // Don't count the end date
        }
    }
}