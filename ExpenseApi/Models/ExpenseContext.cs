using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace ExpenseApi.Models;

public class ExpenseContext: IdentityDbContext<ApplicationUser>
{
    public ExpenseContext(DbContextOptions<ExpenseContext> options)
    : base(options)
    {
        
    }
    public DbSet<ExpenseItem> ExpenseItems {get; set;} = null!;
    public DbSet<RefreshToken> RefreshTokens {get; set;}
}