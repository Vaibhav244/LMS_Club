using System;

namespace LMSAPI_ATTENDANCE.Model
{
    public class EmployeeTypeInfo
    {
        public string EmployeeType { get; set; }
        public bool IsDirectContractor { get; set; }
        public DateTime JoiningDate { get; set; }
        public string EmployeeName { get; set; }
        public string JobTitle { get; set; }

        public bool IsIntern => EmployeeType == "INT" ||
                              (JobTitle != null && JobTitle.ToUpper().Contains("INTERN"));
        public bool IsCWR => EmployeeType == "CWR";
    }
}