using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LMSAPI_ATTENDANCE.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LMSAPI_ATTENDANCE.Repository
{
    public class LeaveRepository : ILeaveRepository
    {
        private readonly MyDataRepository _myDataRepository;
        private readonly ILogger<LeaveRepository> _logger;

        public LeaveRepository(MyDataRepository myDataRepository, ILogger<LeaveRepository> logger)
        {
            _myDataRepository = myDataRepository;
            _logger = logger;
        }

        // Existing methods...
        public async Task<EmployeeTypeInfo> GetEmployeeTypeInfo(int empId)
        {
            return await _myDataRepository.GetEmployeeTypeInfo(empId);
        }

        public async Task<double> CalculateWorkingDays(int empId, DateTime startDate, DateTime endDate, bool includeHolidayWeekoff)
        {
            return await _myDataRepository.CalculateWorkingDays(empId, startDate, endDate, includeHolidayWeekoff);
        }

        public async Task<bool> IsWeekendOrHolidayAsync(DateTime date, int employeeId)
        {
            return await _myDataRepository.IsWeekendOrHolidayAsync(date, employeeId);
        }

        public async Task<double> GetLeavesAlreadyTakenInMonth(int empId, DateTime leaveDate)
        {
            return await _myDataRepository.GetLeavesAlreadyTakenInMonth(empId, leaveDate);
        }

        public async Task<bool> HasAdjacentSickLeave(int empId, DateTime leaveDate)
        {
            return await _myDataRepository.HasAdjacentSickLeave(empId, leaveDate);
        }

        public async Task<double> GetLeavesAlreadyTakenInYear(int empId, int year, int leaveTypeId)
        {
            return await _myDataRepository.GetLeavesAlreadyTakenInYear(empId, year, leaveTypeId);
        }

        public async Task<bool> HasMedicalCertificateUploaded(int empId, DateTime startDate, DateTime endDate)
        {
            return await _myDataRepository.HasMedicalCertificateUploaded(empId, startDate, endDate);
        }

        public async Task<bool> IsSupervisorOnshoreAsync(int empId)
        {
            return await _myDataRepository.IsSupervisorOnshoreAsync(empId);
        }

        // NEW: Direct Contractor specific implementations using SQL queries only
        public async Task<double[]> GetMaximumPermissibleSickLeaves_ForDirectContractor(int empId)
        {
            try
            {
                return await _myDataRepository.GetMaximumPermissibleSickLeaves_ForDirectContractor(empId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sick leave details for direct contractor empId: {EmpId}", empId);
                return new double[] { 0, 0, 12 };
            }
        }

        public async Task<double[]> GetMaximumPermissibleCasualLeaves_ForDirectContractor(int empId)
        {
            try
            {
                return await _myDataRepository.GetMaximumPermissibleCasualLeaves_ForDirectContractor(empId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting casual leave details for direct contractor empId: {EmpId}", empId);
                return new double[] { 0, 0, 18 };
            }
        }

        public async Task<List<DateTime>> GetFirstAndLastDatesOfPeriod(int empId, DateTime start, DateTime end)
        {
            try
            {
                return await Task.FromResult(_myDataRepository.GetFirstAndLastDatesOfPeriod(empId, start, end));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting first and last dates of period for empId: {EmpId}", empId);
                return new List<DateTime> { start, end };
            }
        }

        public async Task<double> GetSickLeavesInExtendedPeriod(int empId, DateTime start, DateTime end)
        {
            try
            {
                return await _myDataRepository.GetSickLeavesInExtendedPeriod(empId, start, end);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sick leaves in extended period for empId: {EmpId}", empId);
                return 0.0;
            }
        }

        public async Task<DateTime> GetEmployeeTransferDate(int empId)
        {
            try
            {
                return await _myDataRepository.GetEmployeeTransferDate(empId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transfer date for empId: {EmpId}", empId);
                return DateTime.MinValue;
            }
        }
    }
}