namespace LMSAPI_ATTENDANCE.Model
{
    public class LeaveApplicationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string LeaveId { get; set; }
        public bool IsAutoApproved { get; set; } = false;
    }
}