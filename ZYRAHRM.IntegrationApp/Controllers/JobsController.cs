using Hangfire;
using Microsoft.AspNetCore.Mvc;
using ZyraHangfireService;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {
        //[HttpGet("run")]
        //public IActionResult RunCheckInJob()
        //{
        //    BackgroundJob.Enqueue<JobService>(x => x.CheckIn());
        //    return Ok("Check In Job triggered");
        //}

        //[HttpGet("CheckOut")]
        //public IActionResult RunCheckOutJob()
        //{
        //    BackgroundJob.Enqueue<JobService>(x => x.CheckOut());
        //    return Ok("Check Out Job triggered");
        //}

        [HttpGet("syncAttendanceCheckin")]
        public IActionResult SyncCheckInAttendance()
        {
            BackgroundJob.Enqueue<AttendanceJobService>(
                x => x.SyncCheckInAttendanceAsync());

            return Ok("Attendance sync triggered");
        }

        [HttpGet("syncAttendanceCheckOut")]
        public IActionResult SyncCheckOutAttendance()
        {
            BackgroundJob.Enqueue<AttendanceJobService>(
                x => x.SyncCheckOutAttendanceAsync());

            return Ok("Attendance sync triggered");
        }
    }
}
