using LMSAPI_ATTENDANCE.CustomValidation;
using System.ComponentModel.DataAnnotations;

namespace LMSAPI_ATTENDANCE.Model
{
    public class AttendanceP
    {
        public int emp_id { get; set; }
        public int attendance_type_id { get; set; }
        [Required(ErrorMessage = "StartDate is required.")]
        [DataType(DataType.Date)]
        public DateTime start_date { get; set; }
        [Required(ErrorMessage = "EndDate is required.")]
        [DateRangeValidation(ErrorMessage = "EndDate cannot be less than StartDate.")]
        [DataType(DataType.Date)]
        public DateTime end_date { get; set; }
        public string comments { get; set; }
        public int IncludeHolidayWeekoff { get; set; }
    }
}