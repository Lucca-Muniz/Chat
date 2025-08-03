using FinancialChat.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialChat.Core.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(ApplicationUser user);
        bool ValidateToken(string token);
    }
}
