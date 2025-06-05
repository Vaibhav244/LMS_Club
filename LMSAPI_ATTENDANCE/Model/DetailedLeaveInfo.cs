using System.Text.Json.Serialization;

namespace LMSAPI_ATTENDANCE.Model
{
    public class DetailedLeaveInfo
    {
        public int EmployeeId { get; set; }
        public DateTime AsOfDate { get; set; }
        public string Manager { get; set; }
        public DateTime JoiningDate { get; set; }
        public double CarriedForwardLeaves { get; set; }
        public double CreditedLeaves { get; set; }
        public double EarnedLeaves { get; set; }
        public double TotalEarnedLeaves { get; set; }
        public double ApprovedLeaves { get; set; }
        public double PendingLeaves { get; set; }
        public double TotalLeavesAvailed { get; set; }
        public double AdvanceLeaves { get; set; }
        public double BalanceLeaves { get; set; }
        public double MaxPermissibleBalance { get; set; }
        public double MaxBalance { get; set; }
        public double EncashableLeaves { get; set; }

        [JsonIgnore]
        public object ComplexProperties { get; set; }
    }
}