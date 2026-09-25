using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;

namespace Zyra.LantimeServiceApp.Models
{
    public class EmployeeMapperDto
    {
        [Index(2)]
        public int UserId { get; set; }
        
        [Index(0)]
        public string EmployeeCode { get; set; }

        [Index(1)]
        public string EmployeeName { get; set; }

        [Index(3)]
        public string? BiometricUserId { get; set; }
    }
}
