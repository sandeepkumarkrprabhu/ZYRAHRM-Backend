using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IAttendanceApiService
    {
        /// <summary>
        /// Sends attendance action (checkin/checkout) to HRMS system
        /// </summary>
        Task<bool> SendAsync(AttendanceAPIDto request);
    }
}
