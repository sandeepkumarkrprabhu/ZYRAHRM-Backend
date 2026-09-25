using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zyra.LantimeServiceApp.Models
{
    public class LoginResult
    {
        public int User_Id { get; set; }
        public int? Employee_Id { get; set; }
        public string Access_Token { get; set; }
        public string Refresh_Token { get; set; }
        public string Token_Type { get; set; }
        public int Access_Token_Expiry_Time { get; set; } // in seconds
        public int Refresh_Token_Expiry_Time { get; set; }
    }
}
