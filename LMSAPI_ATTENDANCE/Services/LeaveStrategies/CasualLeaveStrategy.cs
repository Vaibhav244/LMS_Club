using System;
using System.Threading.Tasks;
using LMSAPI_ATTENDANCE.Model;
using LMSAPI_ATTENDANCE.Repository;
using Microsoft.Extensions.Configuration;

namespace LMSAPI_ATTENDANCE.Services.LeaveStrategies
{
    public class CasualLeaveStrategy : ILeaveStrategy
    {
        private readonly IConfiguration _configuration;

        public CasualLeaveStrategy(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsEligible(EmployeeTypeInfo employeeInfo)
        {
            if (employeeInfo.EmployeeType == "CWR" ||
                employeeInfo.IsIntern ||
                (employeeInfo.JobTitle != null && employeeInfo.JobTitle.ToUpper().Contains("INTERNS")) ||
                employeeInfo.IsDirectContractor)
            {
                return true;
            }

            return false;
        }

        public async Task<LeaveValidationResult> ValidateApplication(int empId, Leave application, ILeaveRepository repository)
        {
            try
            {
                var employeeInfo = await repository.GetEmployeeTypeInfo(empId);
                if (!IsEligible(employeeInfo))
                {
                    return LeaveValidationResult.Failure("Regular employees are not eligible for casual leave. Please apply for PTO instead.");
                }

                if (application.start_date < employeeInfo.JoiningDate)
                {
                    return LeaveValidationResult.Failure("Leave cannot be applied before joining date.");
                }

                var transferDate = await repository.GetEmployeeTransferDate(empId);
                if (transferDate != DateTime.MinValue && (application.start_date < transferDate && transferDate < DateTime.Now))
                {
                    return LeaveValidationResult.Failure($"Please contact HR to apply leave on dates prior to your Transfer Date: {transferDate:dd-MMM-yyyy}");
                }

                var appliedDateLimit = _configuration.GetValue<int>("LeaveSettings:Common:AppliedDateLimitDays", 6);
                if (application.start_date < DateTime.Today)
                {
                    var workingDaysDifference = CountWorkingDays(application.start_date, DateTime.Today);
                    if (workingDaysDifference > appliedDateLimit)
                    {
                        return LeaveValidationResult.Failure($"Casual leaves cannot be applied later than {appliedDateLimit} working days.");
                    }
                }

                bool includeHolidays = application.includeHolidayWeekoff == 1;
                double workingDays = await repository.CalculateWorkingDays(empId, application.start_date, application.end_date, includeHolidays);

                if (workingDays <= 0)
                {
                    return LeaveValidationResult.Failure("Leave period does not contain any working days.");
                }

                // Handle half-day logic
                if (application.helf_leave == 1)
                {
                    // Interns cannot take half-day leaves
                    if (IsIntern(employeeInfo))
                    {
                        return LeaveValidationResult.Failure("Interns cannot take half-day leaves.");
                    }
                    workingDays = 0.5;
                }

                // Check for adjacent/clubbed leave restrictions
                var periodDates = await repository.GetFirstAndLastDatesOfPeriod(empId, application.start_date, application.end_date);
                var sickLeavesTaken = await repository.GetSickLeavesInExtendedPeriod(empId, periodDates[0].AddDays(-1), periodDates[1].AddDays(1));

                if (sickLeavesTaken > 0)
                {
                    return LeaveValidationResult.Failure("Casual leave can not be clubbed with sick leave.");
                }

                // Employee type-specific validations
                if (IsIntern(employeeInfo))
                {
                    return await ValidateInternLeave(empId, application, workingDays, repository);
                }
                else if (employeeInfo.IsDirectContractor)
                {
                    return await ValidateDirectContractorLeave(empId, application, workingDays, repository);
                }
                else if (employeeInfo.EmployeeType == "CWR")
                {
                    return await ValidateCWRLeave(empId, application, workingDays, repository);
                }

                // Should never get here as eligibility check is already done
                return LeaveValidationResult.Failure("Unknown employee type. Please contact HR.");
            }
            catch (Exception ex)
            {
                return LeaveValidationResult.Failure($"An error occurred while validating your leave request: {ex.Message}");
            }
        }

        private async Task<LeaveValidationResult> ValidateDirectContractorLeave(int empId, Leave application, double workingDays, ILeaveRepository repository)
        {
            // Get direct contractor casual leave details
            var casualLeaveDetails = await repository.GetMaximumPermissibleCasualLeaves_ForDirectContractor(empId);
            double earnedTotal = casualLeaveDetails[0];
            double availedTotal = casualLeaveDetails[1];
            double maxPermissible = casualLeaveDetails[2];
            double availableBalance = earnedTotal - availedTotal;

            // Check annual limit (18 days max as per policy)
            if (availedTotal + workingDays > 18.0)
            {
                return LeaveValidationResult.Failure(
                    $"Annual casual leave limit exceeded. Maximum allowed: 18 days per calendar year. ");
            }

            // Check available balance (earned leave only)
            if (workingDays > availableBalance)
            {
                return LeaveValidationResult.Failure(
                    $"Insufficient casual leave balance. " +
                    $"Earned: {earnedTotal:F1} days, " +
                    $"Already taken/pending: {availedTotal:F1} days, " +
                    $"Available: {availableBalance:F1} days, Requested: {workingDays:F1} days.");
            }

            return LeaveValidationResult.Success();
        }

        private async Task<LeaveValidationResult> ValidateInternLeave(int empId, Leave application, double workingDays, ILeaveRepository repository)
        {
            // Monthly limit: 1 day (unused lapses at month end)
            const double monthlyLimit = 1.0;
            double alreadyTaken = await repository.GetLeavesAlreadyTakenInMonth(empId, application.start_date);

            if (alreadyTaken + workingDays > monthlyLimit)
            {
                return LeaveValidationResult.Failure(
                    $"Casual Leave for Intern can be applied for 1 day only in a month. ");
            }

            // If request spans multiple months, validate each month
            if (application.start_date.Month != application.end_date.Month)
            {
                double nextMonthTaken = await repository.GetLeavesAlreadyTakenInMonth(empId, application.end_date);
                if (nextMonthTaken > 0)
                {
                    return LeaveValidationResult.Failure(
                        $"Casual Leave for Intern can be applied for 1 day only in a month.");
                }
            }

            return LeaveValidationResult.Success();
        }

        private async Task<LeaveValidationResult> ValidateCWRLeave(int empId, Leave application, double workingDays, ILeaveRepository repository)
        {
            // Monthly limit: 1 day (unused lapses at month end)
            const double monthlyLimit = 1.0;
            double alreadyTaken = await repository.GetLeavesAlreadyTakenInMonth(empId, application.start_date);

            if (alreadyTaken + workingDays > monthlyLimit)
            {
                return LeaveValidationResult.Failure(
                    $"CWR employees can take maximum 1 day of casual leave per month. ");
            }

            // If request spans multiple months, validate each month
            if (application.start_date.Month != application.end_date.Month)
            {
                double nextMonthTaken = await repository.GetLeavesAlreadyTakenInMonth(empId, application.end_date);
                if (nextMonthTaken > 0)
                {
                    return LeaveValidationResult.Failure(
                        $"CWR employees can take maximum 1 day of casual leave per month.");
                }
            }

            return LeaveValidationResult.Success();
        }

        public async Task<object> GetLeaveBalance(int empId, ILeaveRepository repository)
        {
            try
            {
                var employeeInfo = await repository.GetEmployeeTypeInfo(empId);

                // Check eligibility first
                if (!IsEligible(employeeInfo))
                {
                    return new { Success = false, Message = "Regular employees are not eligible for casual leave. Please check PTO balance instead." };
                }

                // Direct Contractor Balance
                if (employeeInfo.IsDirectContractor)
                {
                    var casualLeaveDetails = await repository.GetMaximumPermissibleCasualLeaves_ForDirectContractor(empId);
                    double earnedTotal = casualLeaveDetails[0];
                    double availedTotal = casualLeaveDetails[1];
                    double maxPermissible = casualLeaveDetails[2];
                    double remaining = Math.Max(0, earnedTotal - availedTotal);

                    return new
                    {
                        Success = true,
                        EarnedTotal = earnedTotal,
                        AvailedTotal = availedTotal,
                        MaxPermissible = Math.Min(maxPermissible, 18.0), // Annual cap of 18 days
                        Remaining = remaining,
                        AnnualLimit = 18.0,
                        Year = DateTime.Now.Year,
                        EmployeeType = "Direct Contractor",
                        Policy = new
                        {
                            MonthlyEntitlement = "1.5 working days per month",
                            AnnualAccumulation = "Up to 18 working days per calendar year",
                            CreditSchedule = "9 days credited biannually in January and July",
                            CarryForward = "Unused leaves cannot be carried forward to next year",
                            Clubbing = "Cannot be clubbed with sick leaves without explicit approvals"
                        }
                    };
                }

                // Intern Balance
                if (IsIntern(employeeInfo))
                {
                    var currentMonth = DateTime.Now;
                    var alreadyTaken = await repository.GetLeavesAlreadyTakenInMonth(empId, currentMonth);
                    const double monthlyLimit = 1.0;

                    return new
                    {
                        Success = true,
                        MonthlyLimit = monthlyLimit,
                        AlreadyTaken = alreadyTaken,
                        Remaining = Math.Max(0, monthlyLimit - alreadyTaken),
                        Month = currentMonth.ToString("MMMM yyyy"),
                        EmployeeType = "Intern",
                        Policy = new
                        {
                            MonthlyEntitlement = "1 day per month for personal reasons",
                            Lapse = "Unused leave lapses at the end of each month",
                            Clubbing = "Cannot be clubbed with any other type of leave",
                            HalfDay = "Half-day leaves not allowed"
                        }
                    };
                }

                // CWR Balance
                if (employeeInfo.EmployeeType == "CWR")
                {
                    var currentMonth = DateTime.Now;
                    var alreadyTaken = await repository.GetLeavesAlreadyTakenInMonth(empId, currentMonth);
                    const double monthlyLimit = 1.0;

                    return new
                    {
                        Success = true,
                        MonthlyLimit = monthlyLimit,
                        AlreadyTaken = alreadyTaken,
                        Remaining = Math.Max(0, monthlyLimit - alreadyTaken),
                        Month = currentMonth.ToString("MMMM yyyy"),
                        EmployeeType = "CWR (Contractor)",
                        Policy = new
                        {
                            MonthlyEntitlement = "1 day per month",
                            Lapse = "Unused leave lapses at the end of each month"
                        }
                    };
                }

                // This should never be reached due to eligibility check
                return new { Success = false, Message = "Unknown employee type" };
            }
            catch (Exception ex)
            {
                return new { Success = false, Message = $"Error calculating leave balance: {ex.Message}" };
            }
        }

        private bool IsIntern(EmployeeTypeInfo employeeInfo)
        {
            return employeeInfo.EmployeeType == "INT" ||
                   (employeeInfo.JobTitle != null && employeeInfo.JobTitle.ToUpper().Contains("INTERN"));
        }

        private int CountWorkingDays(DateTime startDate, DateTime endDate)
        {
            int workingDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    workingDays++;
            }
            return workingDays - 1; // Don't count the end date
        }
    }
}