using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Handover_2.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var logger = serviceProvider.GetRequiredService<ILogger<IdentityUser>>();

            // Create roles
            string[] roleNames = { "Supervisor", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    logger.LogInformation($"Role {roleName} created: {result.Succeeded}");
                }
            }

            // Create admin user
            var adminEmail = "admin@admin.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, "Admin@123");
                logger.LogInformation($"Admin user created: {createResult.Succeeded}");

                if (createResult.Succeeded)
                {
                    var roleResult = await userManager.AddToRoleAsync(adminUser, "Supervisor");
                    logger.LogInformation($"Admin role assigned: {roleResult.Succeeded}");
                }
            }
            else
            {
                // Ensure existing admin has Supervisor role
                if (!await userManager.IsInRoleAsync(adminUser, "Supervisor"))
                {
                    var roleResult = await userManager.AddToRoleAsync(adminUser, "Supervisor");
                    logger.LogInformation($"Admin role assigned to existing user: {roleResult.Succeeded}");
                }
            }

            // Verify role assignment
            var roles = await userManager.GetRolesAsync(adminUser);
            logger.LogInformation($"Admin user roles: {string.Join(", ", roles)}");
        }
    }
}
