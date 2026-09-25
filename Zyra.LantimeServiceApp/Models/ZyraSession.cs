using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zyra.LantimeServiceApp.Models
{
    public class ZyraSession
    {
        public string Token { get; set; }
        public DateTime Expiry { get; set; }    
    }
}
