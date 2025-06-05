using LMSAPI_ATTENDANCE.Model;
using LMSAPI_ATTENDANCE.Repository;
using LMSAPI_ATTENDANCE.Services.LeaveStrategies;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMSAPI_ATTENDANCE.Services
{
    public class LeaveApplicationService
    {
        private readonly MyDataRepository _repository;
        private readonly ILeaveRepository _leaveRepository;
        private readonly Dictionary<int, ILeaveStrategy> _leaveStrategies;
        private readonly IConfiguration _configuration;
        private readonly LeaveValidationService _validationService;

        public LeaveApplicationService(
            MyDataRepository repository,
            ILeaveRepository leaveRepository,
            IConfiguration configuration,
            LeaveValidationService validationService)
        {
            _repository = repository;
            _leaveRepository = leaveRepository;
            _configuration = configuration;
            _validationService = validationService;

            _leaveStrategies = new Dictionary<int, ILeaveStrategy>
            {
                { (int)LeaveType.SickLeaveNew, new SickLeaveStrategy(_configuration) },
                { (int)LeaveType.PersonalHoliday, new PersonalHolidayStrategy(_configuration) },
                { (int)LeaveType.CasualLeave, new CasualLeaveStrategy(_configuration) }
            };
        }

        public async Task<LeaveApplicationResult> ApplyLeaveAsync(Leave leaveApplication)
        {
            try
            {
                // Block deprecated SickLeave type
                if (leaveApplication.leave_type_id == (int)LeaveType.SickLeave)
                {
                    return new LeaveApplicationResult
                    {
                        Success = false,
                        Message = "This leave type is not applicable. Please use the new Sick Leave option."
                    };
                }
                var employeeInfo = await _leaveRepository.GetEmployeeTypeInfo(leaveApplication.emp_id);
                if (string.IsNullOrEmpty(employeeInfo.EmployeeType))
                {
                    return new LeaveApplicationResult
                    {
                        Success = false,
                        Message = "Employee not found or inactive."
                    };
                }

                if (!_leaveStrategies.TryGetValue(leaveApplication.leave_type_id, out var strategy))
                {
                    return new LeaveApplicationResult
                    {
                        Success = false,
                        Message = $"Leave type {leaveApplication.leave_type_id} is not supported or employee is not eligible."
                    };
                }

                if (!strategy.IsEligible(employeeInfo))
                {
                    return new LeaveApplicationResult
                    {
                        Success = false,
                        Message = "Employee is not eligible for this leave type."
                    };
                }

                var overlapResult = await _validationService.ValidateOverlaps(
                    leaveApplication.emp_id,
                    leaveApplication.start_date,
                    leaveApplication.end_date,
                    leaveApplication.leave_type_id);

                if (!overlapResult.IsValid)
                {
                    return new LeaveApplicationResult
                    {
                        Success = false,
                        Message = overlapResult.ErrorMessage
                    };
                }

                var strategyValidationResult = await strategy.ValidateApplication(
                    leaveApplication.emp_id,
                    leaveApplication,
                    _leaveRepository);

                if (!strategyValidationResult.IsValid)
                {
                    return new LeaveApplicationResult
                    {
                        Success = false,
                        Message = strategyValidationResult.ErrorMessage
                    };
                }

                var appliedDateLimit = _configuration.GetValue<int>("LeaveSettings:Common:AppliedDateLimitDays", 6);
                if (!_validationService.ValidateAppliedDate(leaveApplication.start_date, appliedDateLimit))
                {
                    return new LeaveApplicationResult
                    {
                        Success = false,
                        Message = $"Leaves cannot be applied later than {appliedDateLimit} working days. Please refer to India Leave policy for more details."
                    };
                }

                var result = await _repository.InsertLeaveAsync(leaveApplication);

                if (result == "Leave can be applied only for the working days")
                {
                    return new LeaveApplicationResult { Success = false, Message = result };
                }
                else if (result == "Issue")
                {
                    return new LeaveApplicationResult
                    {
                        Success = false,
                        Message = "Failed to submit leave application. Please try again."
                    };
                }

                return new LeaveApplicationResult
                {
                    Success = true,
                    LeaveId = result,
                    Message = "Leave application submitted successfully!."
                };
            }
            catch (Exception ex)
            {
                return new LeaveApplicationResult
                {
                    Success = false,
                    Message = $"An error occurred: {ex.Message}"
                };
            }
        }

        public async Task<object> GetLeaveBalanceAsync(int empId, int leaveTypeId)
        {
            try
            {
                // Route specific types to CasualLeaveStrategy
                int strategyLeaveType = leaveTypeId;
                if (leaveTypeId == 26 || leaveTypeId == 2)
                {
                    strategyLeaveType = (int)LeaveType.CasualLeave;
                }

                if (_leaveStrategies.TryGetValue(strategyLeaveType, out var strategy))
                {
                    var employeeInfo = await _leaveRepository.GetEmployeeTypeInfo(empId);

                    if (!strategy.IsEligible(employeeInfo) && !(leaveTypeId == 26 || leaveTypeId == 2))
                    {
                        return new { Success = false, Message = "Employee is not eligible for this leave type." };
                    }

                    var balance = await strategy.GetLeaveBalance(empId, _leaveRepository);
                    return balance;
                }

                return new { Success = false, Message = "Leave type not supported." };
            }
            catch (Exception ex)
            {
                return new { Success = false, Message = $"An error occurred: {ex.Message}" };
            }
        }

        public async Task<bool> RequiresAutoApprovalAsync(int empId)
        {
            try
            {
                return await _leaveRepository.IsSupervisorOnshoreAsync(empId);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}