using System;
using System.Threading.Tasks;
using LMSAPI_ATTENDANCE.Model;
using LMSAPI_ATTENDANCE.Repository;
using Microsoft.Extensions.Configuration;

namespace LMSAPI_ATTENDANCE.Services.LeaveStrategies
{
    public class PersonalHolidayStrategy : ILeaveStrategy
    {
        private readonly IConfiguration _configuration;

        public PersonalHolidayStrategy(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsEligible(EmployeeTypeInfo employeeInfo)
        {
            if (IsIntern(employeeInfo))
            {
                return false;
            }

            return !string.IsNullOrEmpty(employeeInfo.EmployeeType) &&
                   !string.IsNullOrEmpty(employeeInfo.EmployeeName);
        }

        public async Task<LeaveValidationResult> ValidateApplication(int empId, Leave application, ILeaveRepository repository)
        {
            try
            {
                var employeeInfo = await repository.GetEmployeeTypeInfo(empId);

                if (!IsEligible(employeeInfo))
                {
                    if (IsIntern(employeeInfo))
                    {
                        return LeaveValidationResult.Failure("Interns are not eligible for personal holiday. Only Casual Leave and Intern Exam Leave are allowed for interns.");
                    }
                    return LeaveValidationResult.Failure("Employee is not eligible for personal holiday.");
                }

                if (application.start_date < employeeInfo.JoiningDate)
                {
                    return LeaveValidationResult.Failure($"Leave cannot be applied before joining date ({employeeInfo.JoiningDate:yyyy-MM-dd}).");
                }

                bool includeHolidays = application.includeHolidayWeekoff == 1;
                double workingDays = await repository.CalculateWorkingDays(empId, application.start_date, application.end_date, includeHolidays);

                if (workingDays <= 0)
                {
                    return LeaveValidationResult.Failure("Leave period does not contain any working days.");
                }

                if (application.helf_leave == 1)
                {
                    workingDays = 0.5;
                }

                // Advance notice requirement
                var advanceNoticeRequired = _configuration.GetValue<int>("LeaveSettings:PersonalHoliday:AdvanceNoticeDays", 1);
                if (advanceNoticeRequired > 0 && application.start_date > DateTime.Today)
                {
                    var daysDifference = (application.start_date - DateTime.Today).TotalDays;
                    if (daysDifference < advanceNoticeRequired)
                    {
                        return LeaveValidationResult.Failure($"Personal holiday requires {advanceNoticeRequired} days advance notice.");
                    }
                }

                // Annual limit validation
                var annualLimit = _configuration.GetValue<double>("LeaveSettings:PersonalHoliday:AnnualLimit", 2.0);
                if (annualLimit > 0)
                {
                    var currentYear = application.start_date.Year;
                    var alreadyTaken = await repository.GetLeavesAlreadyTakenInYear(empId, currentYear, (int)LeaveType.PersonalHoliday);

                    if (alreadyTaken + workingDays > annualLimit)
                    {
                        var remaining = Math.Max(0, annualLimit - alreadyTaken);
                        return LeaveValidationResult.Failure(
                            $"Personal holiday annual limit exceeded. Limit: {annualLimit} days, " +
                            $"Already taken: {alreadyTaken} days, Remaining: {remaining} days, " +
                            $"Requested: {workingDays} days.");
                    }
                }

                return LeaveValidationResult.Success();
            }
            catch (Exception ex)
            {
                return LeaveValidationResult.Failure("An error occurred while validating your personal holiday request. Please try again later.");
            }
        }

        public async Task<object> GetLeaveBalance(int empId, ILeaveRepository repository)
        {
            try
            {
                var employeeInfo = await repository.GetEmployeeTypeInfo(empId);

                if (IsIntern(employeeInfo))
                {
                    return new { Success = false, Message = "Interns are not eligible for personal holiday. Only Casual Leave and Intern Exam Leave are allowed." };
                }

                var annualLimit = _configuration.GetValue<double>("LeaveSettings:PersonalHoliday:AnnualLimit", 2.0);
                var currentYear = DateTime.Now.Year;

                var alreadyTaken = await repository.GetLeavesAlreadyTakenInYear(empId, currentYear, (int)LeaveType.PersonalHoliday);

                return new
                {
                    Success = true,
                    AnnualLimit = annualLimit,
                    AlreadyTaken = alreadyTaken,
                    Remaining = Math.Max(0, annualLimit - alreadyTaken),
                    Year = currentYear,
                    Policy = new
                    {
                        AdvanceNoticeDays = _configuration.GetValue<int>("LeaveSettings:PersonalHoliday:AdvanceNoticeDays", 1),
                        AllowHalfDay = _configuration.GetValue<bool>("LeaveSettings:PersonalHoliday:AllowHalfDay", true)
                    }
                };
            }
            catch
            {
                return new { Success = false, Message = "Error calculating personal holiday balance." };
            }
        }

        private bool IsIntern(EmployeeTypeInfo employeeInfo)
        {
            return employeeInfo.EmployeeType == "INT" ||
                   (employeeInfo.JobTitle != null && employeeInfo.JobTitle.ToUpper().Contains("INTERN"));
        }
    }
}