using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IJobService
    {
        DateTime GetLastSyncTime();
        TimeSpan GetDayTimespan();
    }
}
