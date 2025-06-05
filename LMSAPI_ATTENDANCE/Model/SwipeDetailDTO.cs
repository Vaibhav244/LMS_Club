using System;

namespace LMSAPI_ATTENDANCE.Model
{
    public class SwipeDetailDTO
    {
        public DateTime Time { get; set; }
        public DateTime Day { get; set; }
        public string Swipe_Direction { get; set; }
        public int Emp_Id { get; set; }
    }
}