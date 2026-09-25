using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface ISessionService
    {
        Task<string> GetTokenAsync();
    }
}
