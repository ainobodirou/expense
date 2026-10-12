using ExpenseApi.ViewModels.Authentication;
using Microsoft.AspNetCore.Identity;

public class AppDbInitializer
{
    public static async Task SeedRoles(IApplicationBuilder applicationBuilder)
    {
        using(var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
        {
            var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roleManager.RoleExistsAsync(UserRoles.Admin))
                await roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
            
            if (!await roleManager.RoleExistsAsync(UserRoles.User))
                await roleManager.CreateAsync(new IdentityRole(UserRoles.User));
        }
    }
}