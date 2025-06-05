using LMSAPI_ATTENDANCE.Model;
using LMSAPI_ATTENDANCE.Repository;
using System.Threading.Tasks;

namespace LMSAPI_ATTENDANCE.Services
{
    public interface ILeaveValidationStrategy
    {
        Task<LeaveValidationResult> ValidateApplication(int empId, Leave application, ILeaveRepository repository);
    }
}