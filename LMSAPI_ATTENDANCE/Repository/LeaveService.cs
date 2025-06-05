using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LMSAPI_ATTENDANCE.Repository
{
    public static class LeaveService
    {
        public static IConfiguration Configuration { get; set; }

        static LeaveService()
        {
            if (Configuration == null)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                Configuration = builder.Build();
            }
        }

        // Get leaves for employee
        public static DataSet GetLeavesOfEmployee(int empId)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddScoped<MyDataRepository>();

            var serviceProvider = services.BuildServiceProvider();
            var repository = serviceProvider.GetService<MyDataRepository>();

            return repository.GetLeavesOfEmployee(empId);
        }

        // Get attendance for employee
        public static DataSet GetAttendanceOfEmployee(int empId)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddScoped<MyDataRepository>();

            var serviceProvider = services.BuildServiceProvider();
            var repository = serviceProvider.GetService<MyDataRepository>();

            return repository.GetAttendanceOfEmployee(empId);
        }

        // Get employee count
        public static int GetEmployeeCount(string query)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddScoped<DbLayer>();

            var serviceProvider = services.BuildServiceProvider();
            var repository = serviceProvider.GetService<DbLayer>();

            return repository.GetEmployeeCount(query);
        }

        // Get working location
        public static string GetWorkingLocation(string query)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddScoped<DbLayer>();

            var serviceProvider = services.BuildServiceProvider();
            var repository = serviceProvider.GetService<DbLayer>();

            return repository.GetWorkingLocation(query);
        }

        // Get EL manually updated rows
        public static int noOfRowsELManuallyUpdated(string query)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddScoped<DbLayer>();

            var serviceProvider = services.BuildServiceProvider();
            var repository = serviceProvider.GetService<DbLayer>();

            return repository.noOfRowsELManuallyUpdated(query);
        }
    }
}