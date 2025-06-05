namespace LMSAPI_ATTENDANCE.Model
{
    public class EmployeeAttendanceDTO
    {
        private int emp_Id;
        private SwipeDetailsCollection swipeDetails;
        private SwipeDetailsCollection tempCardDetail;
        private SwipeDetailsCollection tempCardEntry;
        private ReasonDetailDTO reasonDetails;

        public int Emp_Id
        {
            get { return emp_Id; }
            set { emp_Id = value; }
        }

        public SwipeDetailsCollection SwipeDetails
        {
            get { return swipeDetails; }
            set { swipeDetails = value; }
        }

        public SwipeDetailsCollection TempCardDetail
        {
            get { return tempCardDetail; }
            set { tempCardDetail = value; }
        }

        public SwipeDetailsCollection TempCardEntryDetail
        {
            get { return tempCardEntry; }
            set { tempCardEntry = value; }
        }

        public ReasonDetailDTO ReasonDetails
        {
            get { return reasonDetails; }
            set { reasonDetails = value; }
        }

        public EmployeeAttendanceDTO()
        {
            swipeDetails = new SwipeDetailsCollection();
            tempCardDetail = new SwipeDetailsCollection();
            reasonDetails = new ReasonDetailDTO();
        }
    }
}