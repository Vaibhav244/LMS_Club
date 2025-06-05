using System;
using System.Threading.Tasks;
using LMSAPI_ATTENDANCE.Model;
using LMSAPI_ATTENDANCE.Repository;
using Microsoft.Extensions.Configuration;

namespace LMSAPI_ATTENDANCE.Services.LeaveStrategies
{
    public class SickLeaveStrategy : ILeaveStrategy
    {
        private readonly IConfiguration _configuration;

        public SickLeaveStrategy(IConfiguration configuration)
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
                        return LeaveValidationResult.Failure("Interns are not eligible for sick leave. Only Casual Leave and Intern Exam Leave are allowed for interns.");
                    }
                    return LeaveValidationResult.Failure("Employee is not eligible for sick leave.");
                }

                if (application.start_date < employeeInfo.JoiningDate)
                {
                    return LeaveValidationResult.Failure($"Sick leave cannot be applied before joining date ({employeeInfo.JoiningDate:yyyy-MM-dd}).");
                }

                bool includeHolidays = application.includeHolidayWeekoff == 1;
                double workingDays = await repository.CalculateWorkingDays(empId, application.start_date, application.end_date, includeHolidays);

                if (workingDays <= 0)
                {
                    return LeaveValidationResult.Failure("Sick leave period does not contain any working days.");
                }

                if (application.helf_leave == 1)
                {
                    workingDays = 0.5;
                }

                // Direct contractor validation
                if (employeeInfo.IsDirectContractor)
                {
                    var sickLeaveDetails = await repository.GetMaximumPermissibleSickLeaves_ForDirectContractor(empId);
                    double earnedTillDate = sickLeaveDetails[0];
                    double availedTotal = sickLeaveDetails[1];
                    double maxPermissible = sickLeaveDetails[2];

                    // Annual limit check
                    if (availedTotal + workingDays > maxPermissible)
                    {
                        var casualLeaveDetails = await repository.GetMaximumPermissibleCasualLeaves_ForDirectContractor(empId);
                        double casualEarned = casualLeaveDetails[0];
                        double casualAvailed = casualLeaveDetails[1];
                        double casualRemaining = casualEarned - casualAvailed;

                        if (casualRemaining >= workingDays)
                        {
                            return LeaveValidationResult.Failure(
                                $"Sick leave annual limit of {maxPermissible} days exceeded. " +
                                $"You can use your accrued casual leave balance of {casualRemaining} days for this absence.");
                        }
                        else
                        {
                            return LeaveValidationResult.Failure(
                                "You are not allowed to take this leave as both sick leave and casual leave balances are exhausted.");
                        }
                    }

                    // Medical certificate warning for 3+ days
                    if (workingDays >= 3)
                    {
                        return LeaveValidationResult.Warning(
                            "For sick leave of 3 or more consecutive days, you may be required to submit a medical certificate " +
                            "issued by a doctor affiliated with a government hospital or insurance vendor empaneled hospitals. " +
                            "Your leave application will proceed for approval.");
                    }
                }

                return LeaveValidationResult.Success();
            }
            catch (Exception ex)
            {
                return LeaveValidationResult.Failure("An error occurred while validating your sick leave request. Please try again later.");
            }
        }

        public async Task<object> GetLeaveBalance(int empId, ILeaveRepository repository)
        {
            try
            {
                var employeeInfo = await repository.GetEmployeeTypeInfo(empId);

                if (IsIntern(employeeInfo))
                {
                    return new
                    {
                        Success = false,
                        Message = "Interns are not eligible for sick leave. Only Casual Leave and Intern Exam Leave are allowed."
                    };
                }

                if (employeeInfo.IsDirectContractor)
                {
                    var sickLeaveDetails = await repository.GetMaximumPermissibleSickLeaves_ForDirectContractor(empId);
                    double earnedTillDate = sickLeaveDetails[0];
                    double availedTotal = sickLeaveDetails[1];
                    double maxPermissible = sickLeaveDetails[2];
                    double remaining = Math.Max(0, maxPermissible - availedTotal);

                    return new
                    {
                        Success = true,
                        EarnedTillDate = earnedTillDate,
                        AvailedTotal = availedTotal,
                        MaxPermissible = maxPermissible,
                        Remaining = remaining,
                        Year = DateTime.Now.Year,
                        EmployeeType = "Direct Contractor",
                        Note = "Sick leaves that are not utilized in a year cannot be carried forward to the next year. All unused sick leaves as on December 31 will lapse.",
                        MedicalCertificateRequired = "Medical certificate required for sick leave of 3 days or more."
                    };
                }

                return new
                {
                    Success = true,
                    Message = "Sick leave is available as per medical requirements.",
                    IsUnlimited = true,
                    Note = "Sick leave follows standard company policy."
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Success = false,
                    Message = "Error calculating sick leave balance. Please try again later."
                };
            }
        }

        private bool IsIntern(EmployeeTypeInfo employeeInfo)
        {
            return employeeInfo.EmployeeType == "INT" ||
                   (employeeInfo.JobTitle != null && employeeInfo.JobTitle.ToUpper().Contains("INTERN"));
        }
    }
}