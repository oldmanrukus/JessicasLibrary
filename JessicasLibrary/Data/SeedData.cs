using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace JessicasLibrary.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

            // 1) Ensure "Admin" role exists
            const string adminRole = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRole));
            }

            // 2) Ensure the hard‐coded admin user exists
            const string adminUserName = "oldmanrukus";
            const string adminPassword = "LoganMcl123!!!";
            const string adminEmail = "oldmanrukus@example.com";

            var adminUser = await userManager.FindByNameAsync(adminUserName);
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (!createResult.Succeeded)
                {
                    throw new Exception("Failed to create admin user: " +
                        string.Join("; ", createResult.Errors.Select(e => e.Description)));
                }
            }

            // 3) Assign that user to Admin role
            if (!await userManager.IsInRoleAsync(adminUser, adminRole))
                await userManager.AddToRoleAsync(adminUser, adminRole);

            // 4) Promote your live account if it already exists
            const string liveEmail = "rukzero@gmail.com";
            var liveUser = await userManager.FindByEmailAsync(liveEmail);
            if (liveUser != null && !await userManager.IsInRoleAsync(liveUser, adminRole))
            {
                await userManager.AddToRoleAsync(liveUser, adminRole);
            }
        }
    }
}
