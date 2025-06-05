namespace LMSAPI_ATTENDANCE.Model
{
    public class ReasonDetailDTO
    {
        private int emp_Id;
        private int attendance_Type_Id;
        private string attendance_Type_Name;
        private string status;
        private string comments;
        private DateTime reason_Date;
        private bool isReasonPresent = false;

        public int Emp_Id
        {
            get { return emp_Id; }
            set { emp_Id = value; }
        }

        public int Attendance_Type_Id
        {
            get { return attendance_Type_Id; }
            set { attendance_Type_Id = value; }
        }

        public string Comments
        {
            get { return comments; }
            set { comments = value; }
        }

        public string Attendance_Type_Name
        {
            get { return attendance_Type_Name; }
            set { attendance_Type_Name = value; }
        }

        public DateTime Reason_Date
        {
            get { return reason_Date; }
            set { reason_Date = value; }
        }

        public bool IsReasonPresent
        {
            get { return isReasonPresent; }
            set { isReasonPresent = value; }
        }

        public string Status
        {
            get { return status; }
            set { status = value; }
        }
    }
}