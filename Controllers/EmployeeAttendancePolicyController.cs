using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;
using ZyraHangfireModels.PresentationModels;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeAttendancePolicyController : ControllerBase
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<EmployeeAttendancePolicyController> _logger;

        public EmployeeAttendancePolicyController(AttendanceDbContext dbContext, ILogger<EmployeeAttendancePolicyController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<EmployeePolicyDetails>>> Get()
        {
            try
            {
                var policies = await (
                    from employee in _dbContext.EmployeeMappings
                    join policy in _dbContext.EmployeeAttendancePolicies
                        on employee.Id equals policy.EmployeeId
                    select new EmployeePolicyDetails
                    {
                        EmployeeId = employee.Id,
                        EmployeeCode = employee.HRMEmployeeCode,
                        EmployeeName = employee.EmployeeName,

                        AttendancePolicyId = policy.AttendancePolicyId,

                        EffectiveFrom = policy.EffectiveFrom,
                        EffectiveTo = policy.EffectiveTo,

                        AutoCheckInTime = policy.AutoCheckInTime,
                        AutoCheckOutTime = policy.AutoCheckOutTime,

                        IsEnabled = policy.IsEnabled,

                        CreatedOn = policy.CreatedOn,
                        UpdatedOn = policy.UpdatedOn
                    }
                )
                .AsNoTracking()
                .ToListAsync();

                return Ok(policies);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching employee attendance policies.");

                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("assign-default")]
        public async Task<IActionResult> AssignDefaulttoAll()
        {
            try
            {
                // Fetch the default attendance policy
                var defaultPolicy = await _dbContext.AttendancePolicyMasters
                    .FirstOrDefaultAsync(p => p.IsDefault??false);

                if (defaultPolicy == null)
                {
                    return NotFound("Default attendance policy not found.");
                }

                // Fetch employees with existing attendance policies
                var employeesWithPolicy = await _dbContext.EmployeeAttendancePolicies.ToListAsync();

                if (employeesWithPolicy.Any())
                {
                    // Update existing policies
                    foreach (var emp in employeesWithPolicy)
                    {
                        emp.AttendancePolicyId = defaultPolicy.Id;
                    }
                }
                else
                {
                    // No existing policies → create new ones from EmployeeMappings
                    var employeeMasters = await _dbContext.EmployeeMappings.ToListAsync();

                    if (!employeeMasters.Any())
                    {
                        return NotFound("No employees found to assign default policy.");
                    }

                    var newPolicies = employeeMasters.Select(e => new EmployeeAttendancePolicy
                    {
                        EmployeeId = e.Id,
                        AttendancePolicyId = defaultPolicy.Id,
                        EffectiveFrom = DateTime.Now,
                        EffectiveTo = DateTime.MaxValue,
                        AutoCheckInTime = new DateTime(DateTime.Today.Year,DateTime.Today.Month,DateTime.Today.Day,9, 30, 0),
                        AutoCheckOutTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 18, 30, 0),
                        IsEnabled = true,
                        CreatedOn = DateTime.Now,
                        UpdatedOn = DateTime.Now
                    }).ToList();

                    await _dbContext.EmployeeAttendancePolicies.AddRangeAsync(newPolicies);
                }

                // Save changes
                await _dbContext.SaveChangesAsync();

                return Ok("Default attendance policy assigned successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost]
        public async Task<IActionResult> AssignPolicyByEmployeeId(EmployeeAttendancePolicy newEmployeePolicy)
        {
            try
            {
                // Check if the employee exists
                var employee = await _dbContext.EmployeeMappings.FindAsync(newEmployeePolicy.EmployeeId);
                if (employee == null)
                {
                    return NotFound($"Employee with ID {newEmployeePolicy.EmployeeId} not found.");
                }
                
                // Check if the policy exists
                var policy = await _dbContext.AttendancePolicyMasters.FindAsync(newEmployeePolicy.AttendancePolicyId);
                if (policy == null)
                {
                    return NotFound($"Attendance policy with ID {newEmployeePolicy.AttendancePolicyId} not found.");
                }

                // Check if the employee already has a policy assigned
                var existingPolicy = await _dbContext.EmployeeAttendancePolicies
                    .FirstOrDefaultAsync(e => e.EmployeeId == newEmployeePolicy.EmployeeId);
                if (existingPolicy != null)
                {
                    // Update existing policy
                    existingPolicy.AttendancePolicyId = newEmployeePolicy.AttendancePolicyId;
                }
                else
                {
                    // Assign new policy
                    var newPolicyAssignment = new EmployeeAttendancePolicy
                    {
                        EmployeeId = newEmployeePolicy.EmployeeId,
                        AttendancePolicyId = newEmployeePolicy.AttendancePolicyId,
                        EffectiveFrom = newEmployeePolicy.EffectiveFrom,
                        EffectiveTo = newEmployeePolicy.EffectiveTo,
                        AutoCheckInTime = newEmployeePolicy.AutoCheckInTime,
                        AutoCheckOutTime = newEmployeePolicy.AutoCheckOutTime,
                        CreatedOn = DateTime.UtcNow,
                        IsEnabled = true,
                        UpdatedOn = DateTime.UtcNow
                    };
                    await _dbContext.EmployeeAttendancePolicies.AddAsync(newPolicyAssignment);
                }
                // Save changes
                await _dbContext.SaveChangesAsync();
                return Ok($"Attendance policy assigned to employee ID {newEmployeePolicy.EmployeeId} successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{employeeId}")]
        public async Task<IActionResult> UpdatePolicyByEmployeeId(int employeeId, EmployeeAttendancePolicy updatedEmployeePolicy)
        {
            try
            {
                // Check if the employee exists
                var employee = await _dbContext.EmployeeMappings.FindAsync(employeeId);
                if (employee == null)
                {
                    return NotFound($"Employee with ID {employeeId} not found.");
                }

                // Check if the policy exists
                var policy = await _dbContext.AttendancePolicyMasters.FindAsync(updatedEmployeePolicy.AttendancePolicyId);
                if (policy == null)
                {
                    return NotFound($"Attendance policy with ID {updatedEmployeePolicy.AttendancePolicyId} not found.");
                }

                // Update the existing policy
                var existingPolicy = await _dbContext.EmployeeAttendancePolicies
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
                if (existingPolicy == null)
                {
                    return NotFound($"No attendance policy found for employee ID {employeeId}.");
                }

                existingPolicy.AttendancePolicyId = updatedEmployeePolicy.AttendancePolicyId;
                existingPolicy.EffectiveFrom = updatedEmployeePolicy.EffectiveFrom;
                existingPolicy.EffectiveTo = updatedEmployeePolicy.EffectiveTo;
                existingPolicy.AutoCheckInTime = updatedEmployeePolicy.AutoCheckInTime;
                existingPolicy.AutoCheckOutTime = updatedEmployeePolicy.AutoCheckOutTime;
                existingPolicy.IsEnabled = updatedEmployeePolicy.IsEnabled;
                existingPolicy.UpdatedOn = DateTime.UtcNow;

                // Save changes
                await _dbContext.SaveChangesAsync();
                return Ok($"Attendance policy updated for employee ID {employeeId} successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
