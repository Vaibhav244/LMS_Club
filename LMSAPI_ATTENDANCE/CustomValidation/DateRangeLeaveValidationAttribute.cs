using LMSAPI_ATTENDANCE.Model;
using System;
using System.ComponentModel.DataAnnotations;

namespace LMSAPI_ATTENDANCE.CustomValidation
{
    public class DateRangeValidationAttribute : ValidationAttribute
    {
        protected override System.ComponentModel.DataAnnotations.ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var model = (Attendance)validationContext.ObjectInstance;
            if (model.end_date < model.start_date)
            {
                return new System.ComponentModel.DataAnnotations.ValidationResult("EndDate cannot be less than StartDate.");
            }
            return System.ComponentModel.DataAnnotations.ValidationResult.Success;
        }
    }
}