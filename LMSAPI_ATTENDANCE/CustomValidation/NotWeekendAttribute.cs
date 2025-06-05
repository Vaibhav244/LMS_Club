using System;
using System.ComponentModel.DataAnnotations;

namespace LMSAPI_ATTENDANCE.CustomValidation
{
    public class NotWeekendAttribute : ValidationAttribute
    {
        private readonly string _includeHolidayWeekoffPropertyName;

        public NotWeekendAttribute(string includeHolidayWeekoffPropertyName)
        {
            _includeHolidayWeekoffPropertyName = includeHolidayWeekoffPropertyName;
        }

        // Validate that date is not a weekend if holidays/weekoffs are excluded
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime date)
            {
                var prop = validationContext.ObjectType.GetProperty(_includeHolidayWeekoffPropertyName);
                if (prop == null)
                    return new ValidationResult($"Unknown property: {_includeHolidayWeekoffPropertyName}");

                var includeHolidayWeekoff = (int)prop.GetValue(validationContext.ObjectInstance);
                if (includeHolidayWeekoff == 0)
                {
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        return new ValidationResult("Attendance can be applied only for the working days.");
                }
            }
            return ValidationResult.Success;
        }
    }
}