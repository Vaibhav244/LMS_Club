namespace LMSAPI_ATTENDANCE.Model
{
    public class LeaveApplication
    {
        public int LeaveId { get; set; }
        public string LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime AppliedDate { get; set; }
        public double WorkingDays { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
    }
}