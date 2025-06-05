namespace LMSAPI_ATTENDANCE.Model
{
    public class LeaveValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsWarning { get; set; }

        public static LeaveValidationResult Success()
        {
            return new LeaveValidationResult { IsValid = true };
        }

        public static LeaveValidationResult Failure(string errorMessage)
        {
            return new LeaveValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }

        public static LeaveValidationResult Warning(string warningMessage)
        {
            return new LeaveValidationResult
            {
                IsValid = true,
                ErrorMessage = warningMessage,
                IsWarning = true
            };
        }
    }
}