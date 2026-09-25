using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zyra.LantimeServiceApp.Interfaces;

namespace Zyra.LantimeServiceApp.JobService
{
    public class JobService:IJobService
    {
        public DateTime GetLastSyncTime()
        {
            return new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day); // Replace with DB value
            //return new DateTime(DateTime.Today.Year, DateTime.Today.Month, 08);
        }

        public TimeSpan GetDayTimespan()
        {
            return DateTime.Now.TimeOfDay;
            //return new TimeSpan(19, 0, 0); // test code
        }
    }
}
