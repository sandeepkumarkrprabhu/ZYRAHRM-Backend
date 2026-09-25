using System.Data;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.Repository
{
    public class AttendanceRepository
    {
        private readonly IDbService _dbService;

        public AttendanceRepository(IDbService dbService)
        {
            _dbService = dbService;
        }

        public async Task<IEnumerable<AttendanceDto>> GetAttendanceAsync(DateTime fromDate)
        {
            var sql = "sp_GetAttendance";
            return null;
        }
    }
}
