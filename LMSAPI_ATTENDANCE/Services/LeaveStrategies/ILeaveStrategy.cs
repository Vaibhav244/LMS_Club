using LMSAPI_ATTENDANCE.Model;
using LMSAPI_ATTENDANCE.Repository;
using System.Threading.Tasks;

namespace LMSAPI_ATTENDANCE.Services.LeaveStrategies
{
    public interface ILeaveStrategy
    {
        bool IsEligible(EmployeeTypeInfo employeeInfo);
        Task<LeaveValidationResult> ValidateApplication(int empId, Leave application, ILeaveRepository repository);
        Task<object> GetLeaveBalance(int empId, ILeaveRepository repository);
    }
}