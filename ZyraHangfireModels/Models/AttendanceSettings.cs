using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZyraHangfireModels.Models
{
    public class AttendanceSettings
    {
        public TimeSpan CheckInStart { get; set; }
        public TimeSpan CheckInEnd { get; set; }
        public TimeSpan CheckOutStart { get; set; }
        public TimeSpan CheckOutEnd { get; set; }
    }
}
