namespace LMSAPI_ATTENDANCE.Model
{
    public class SwipeDetailsCollection
    {
        private List<SwipeDetailDTO> swipeDetail;
        private bool isDataPresent = false;
        private bool isTempCardUse = false;

        public List<SwipeDetailDTO> SwipeDetail
        {
            get { return swipeDetail; }
            set { swipeDetail = value; }
        }

        public bool IsDataPresent
        {
            get { return isDataPresent; }
            set { isDataPresent = value; }
        }

        public bool IsTempCardUse
        {
            get { return isTempCardUse; }
            set { isTempCardUse = value; }
        }

        public SwipeDetailsCollection()
        {
            swipeDetail = new List<SwipeDetailDTO>();
        }

        public DateTime MinAccessTime()
        {
            if (swipeDetail != null && swipeDetail.Count > 0)
            {
                return swipeDetail[0].Time;
            }
            else
            {
                throw new Exception("SwipeDetailsCollection.MinAccessTime() is failed");
            }
        }

        public DateTime MaxAccessTime()
        {
            if (swipeDetail != null && swipeDetail.Count > 0)
            {
                return swipeDetail[swipeDetail.Count - 1].Time;
            }
            else
            {
                throw new Exception("SwipeDetailsCollection.MaxAccessTime() is failed");
            }
        }

        public DateTime SwipeDate()
        {
            if (swipeDetail != null && swipeDetail.Count > 0)
            {
                return swipeDetail[0].Day;
            }
            else
            {
                throw new Exception("SwipeDetailsCollection.SwipeDate() is failed");
            }
        }

        public string TotalHours()
        {
            if (swipeDetail != null && swipeDetail.Count > 0)
            {
                TimeSpan totalHours = swipeDetail[swipeDetail.Count - 1].Time - swipeDetail[0].Time;
                int hour = totalHours.Hours;
                int minute = totalHours.Minutes;
                if (minute < 10)
                {
                    return hour + ":0" + minute;
                }
                else
                    return hour + ":" + minute;
            }
            else
            {
                throw new Exception("SwipeDetailsCollection.TotalHours() is failed");
            }
        }

        public bool IsSwipeDetailPresent()
        {
            return isDataPresent;
        }
    }
}