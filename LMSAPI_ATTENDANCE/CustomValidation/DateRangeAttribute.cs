using LMSAPI_ATTENDANCE.Model;
using System;
using System.ComponentModel.DataAnnotations;

namespace LMSAPI_ATTENDANCE.CustomValidation
{
    public class DateRangeAttribute : ValidationAttribute
    {
        private readonly int _maxDays;

        public DateRangeAttribute(int maxDays)
        {
            _maxDays = maxDays;
        }

        protected override System.ComponentModel.DataAnnotations.ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var model = (Attendance)validationContext.ObjectInstance;
            if (model.end_date.Subtract(model.start_date).TotalDays > _maxDays)
            {
                return new System.ComponentModel.DataAnnotations.ValidationResult($"The duration between StartDate and EndDate must be no more than {_maxDays} days.");
            }
            return System.ComponentModel.DataAnnotations.ValidationResult.Success;
        }
    }
}