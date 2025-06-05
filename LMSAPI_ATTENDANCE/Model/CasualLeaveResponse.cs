using System;

namespace LMSAPI_ATTENDANCE.Model
{
    public class CasualLeaveResponse
    {
        public string ID { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public double LeaveBalance { get; set; }
        public double DaysApplied { get; set; }
        public string EmployeeType { get; set; }
    }
}