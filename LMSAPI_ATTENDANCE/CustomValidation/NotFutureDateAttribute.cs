using System;
using System.ComponentModel.DataAnnotations;

namespace LMSAPI_ATTENDANCE.CustomValidation
{
    public class NotFutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime dateTime)
            {
                if (dateTime > DateTime.Now)
                {
                    return new ValidationResult("Date cannot be in the future.");
                }
            }
            return ValidationResult.Success;
        }
    }
}
