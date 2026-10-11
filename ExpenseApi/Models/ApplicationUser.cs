
using Microsoft.AspNetCore.Identity;

namespace ExpenseApi.Models;

public class ApplicationUser : IdentityUser
{
    public string? Custom { get; set;}
}
