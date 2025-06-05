namespace LMSAPI_ATTENDANCE.Model
{
    public class CasualLeaveBalance
    {
        public double LeaveBalance { get; set; }
        public double TotalEntitled { get; set; }
        public double AlreadyTaken { get; set; }
        public string Period { get; set; }
    }
}