using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Minecraft_Monitor.Models
{
    public class MinecraftMonitorContext : IdentityDbContext<IdentityUser>
    {
        public MinecraftMonitorContext(DbContextOptions<MinecraftMonitorContext> options) : base(options)
        {
            Database.EnsureCreated();

            if (!Settings.Any())
            {
                Settings.Add(new Settings());
                SaveChanges();
            }

            if (!ServerInfo.Any())
            {
                ServerInfo.Add(new ServerInfo());
                SaveChanges();
            }
        }

        public DbSet<Settings> Settings { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Coordinates> Coordinates { get; set; }
        public DbSet<ServerInfo> ServerInfo { get; set; }

        public static void EnsureAdminAndRolesExist(UserManager<IdentityUser> userManager,
                                                    RoleManager<IdentityRole> roleManager,
                                                    IConfiguration configuration)
        {
            var adminUser = userManager.FindByNameAsync("admin").GetAwaiter().GetResult();
            var adminUserPassword = configuration.GetValue<string>("AdminPassword");

            // Create admin user.
            if (adminUser == null)
            {
                userManager.CreateAsync(new IdentityUser("admin"), adminUserPassword).GetAwaiter().GetResult();
                adminUser = userManager.FindByNameAsync("admin").GetAwaiter().GetResult();
            }
            // Make sure the admin user has the password set in the settings.
            else
            {
                var token = userManager.GeneratePasswordResetTokenAsync(adminUser).GetAwaiter().GetResult();
                userManager.ResetPasswordAsync(adminUser, token, adminUserPassword).GetAwaiter().GetResult();
            }

            if (!roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole("Admin")).GetAwaiter().GetResult();
            }

            if (!userManager.IsInRoleAsync(adminUser, "Admin").GetAwaiter().GetResult())
            {
                userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
            }
        }
    }
}