using Microsoft.AspNetCore.Identity;

namespace smart_feedback.Data
{
    public static class UserSeeder
    {

        public static async Task SeedRolesAndAdminUserAsync(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Seed roles
            await SeedRolesAsync(roleManager);

            // Seed admin user
            await SeedAdminUserAsync(userManager);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { ApplicationRoles.Admin, ApplicationRoles.Lecturer, ApplicationRoles.Moderator };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var identityRole = new IdentityRole(role);
                    var result = await roleManager.CreateAsync(identityRole);

                    if (result.Succeeded)
                    {
                        Console.WriteLine($"Role '{role}' created successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to create role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<IdentityUser> userManager)
        {
            const string adminEmail = "admin@email.com";
            const string adminPassword = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    // Assign Admin role to the admin user
                    await userManager.AddToRoleAsync(adminUser, ApplicationRoles.Admin);
                    Console.WriteLine($"Admin user created successfully: {adminEmail}");
                }
                else
                {
                    Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                // Ensure admin user has the Admin role
                if (!await userManager.IsInRoleAsync(adminUser, ApplicationRoles.Admin))
                {
                    await userManager.AddToRoleAsync(adminUser, ApplicationRoles.Admin);
                    Console.WriteLine($"Admin role assigned to existing user: {adminEmail}");
                }
            }
        }
    }
}

