using Microsoft.AspNetCore.Identity;

namespace FinancialChat.Core.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}