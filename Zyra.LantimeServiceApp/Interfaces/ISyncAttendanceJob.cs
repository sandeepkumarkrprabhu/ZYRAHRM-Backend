using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Server;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface ISyncAttendanceJob
    {
        Task Execute(PerformContext context);
    }
}
