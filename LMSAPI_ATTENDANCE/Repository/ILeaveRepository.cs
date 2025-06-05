using LMSAPI_ATTENDANCE.Model;
using System;
using System.Threading.Tasks;

namespace LMSAPI_ATTENDANCE.Repository
{
    public interface ILeaveRepository
    {
        Task<EmployeeTypeInfo> GetEmployeeTypeInfo(int empId);
        Task<double> CalculateWorkingDays(int empId, DateTime startDate, DateTime endDate, bool includeHolidayWeekoff);
        Task<bool> IsWeekendOrHolidayAsync(DateTime date, int employeeId);
        Task<double> GetLeavesAlreadyTakenInMonth(int empId, DateTime leaveDate);
        Task<bool> HasAdjacentSickLeave(int empId, DateTime leaveDate);
        Task<double> GetLeavesAlreadyTakenInYear(int empId, int year, int leaveTypeId);
        Task<bool> HasMedicalCertificateUploaded(int empId, DateTime startDate, DateTime endDate);
        Task<bool> IsSupervisorOnshoreAsync(int empId);
        Task<double[]> GetMaximumPermissibleSickLeaves_ForDirectContractor(int empId);
        Task<double[]> GetMaximumPermissibleCasualLeaves_ForDirectContractor(int empId);
        Task<List<DateTime>> GetFirstAndLastDatesOfPeriod(int empId, DateTime start, DateTime end);
        Task<double> GetSickLeavesInExtendedPeriod(int empId, DateTime start, DateTime end);

        Task<DateTime> GetEmployeeTransferDate(int empId);

    }
}