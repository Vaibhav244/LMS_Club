using LMSAPI_ATTENDANCE.CustomValidation;
using System;
using System.ComponentModel.DataAnnotations;

namespace LMSAPI_ATTENDANCE.Model
{
    public class Leave
    {
        [Required(ErrorMessage = "Employee ID is required.")]
        public int emp_id { get; set; }

        [Required(ErrorMessage = "Leave type is required.")]
        public int leave_type_id { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        public DateTime start_date { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        [DateRangeLeaveValidation(ErrorMessage = "End date cannot be less than start date.")]
        public DateTime end_date { get; set; }

        [Range(0, 1, ErrorMessage = "Half leave must be 0 or 1.")]
        public int helf_leave { get; set; }

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string reason { get; set; }

        [Range(0, 1, ErrorMessage = "Include holiday/weekoff must be 0 or 1.")]
        public int includeHolidayWeekoff { get; set; }
        public string medicaldocument { get; set; }
    }
}