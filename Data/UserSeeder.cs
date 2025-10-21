using Microsoft.AspNetCore.Identity;

namespace smart_feedback.Data
{
    public static class UserSeeder
    {

        public static async Task SeedRolesAndAdminUserAsync(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Seed roles
            await SeedRolesAsync(roleManager);

            // Seed admin, lecturer, moderator user
            await SeedAdminUserAsync(userManager);
            await SeedLecturerUserAsync(userManager);
            await SeedModeratorUserAsync(userManager);
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

        private static async Task SeedLecturerUserAsync(UserManager<IdentityUser> userManager)
        {
            const string lecturerEmail = "lecturer@email.com";
            const string lecturerPassword = "Lecturer123!";


            var lecturerUser = await userManager.FindByEmailAsync(lecturerEmail);
            if (lecturerUser == null)
            {
                lecturerUser = new IdentityUser
                {
                    UserName = lecturerEmail,
                    Email = lecturerEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(lecturerUser, lecturerPassword);
                if (result.Succeeded)
                {
                    // Assign Admin role to the admin user
                    await userManager.AddToRoleAsync(lecturerUser, ApplicationRoles.Lecturer);
                    Console.WriteLine($"Lecturer user created successfully: {lecturerEmail}");
                }
                else
                {
                    Console.WriteLine($"Failed to create lecturer user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                // Ensure admin user has the Admin role
                if (!await userManager.IsInRoleAsync(lecturerUser, ApplicationRoles.Lecturer))
                {
                    await userManager.AddToRoleAsync(lecturerUser, ApplicationRoles.Lecturer);
                    Console.WriteLine($"Lecturer role assigned to existing user: {lecturerEmail}");
                }
            }
        }

        private static async Task SeedModeratorUserAsync(UserManager<IdentityUser> userManager)
        {
            const string moderatorEmail = "moderator@email.com";
            const string moderatorPassword = "Moderator123!";


            var moderatorUser = await userManager.FindByEmailAsync(moderatorEmail);
            if (moderatorUser == null)
            {
                moderatorUser = new IdentityUser
                {
                    UserName = moderatorEmail,
                    Email = moderatorEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(moderatorUser, moderatorPassword);
                if (result.Succeeded)
                {
                    // Assign Admin role to the admin user
                    await userManager.AddToRoleAsync(moderatorUser, ApplicationRoles.Lecturer);
                    Console.WriteLine($"Moderator user created successfully: {moderatorEmail}");
                }
                else
                {
                    Console.WriteLine($"Failed to create moderator user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                // Ensure admin user has the Admin role
                if (!await userManager.IsInRoleAsync(moderatorUser, ApplicationRoles.Lecturer))
                {
                    await userManager.AddToRoleAsync(moderatorUser, ApplicationRoles.Lecturer);
                    Console.WriteLine($"Moderator role assigned to existing user: {moderatorEmail}");
                }
            }
        }
    }
}

