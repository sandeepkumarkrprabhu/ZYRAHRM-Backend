using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zyra.LantimeServiceApp.Models
{
    public class LoginApiResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public object Error { get; set; }
        public LoginResult Result { get; set; }
    }
}
