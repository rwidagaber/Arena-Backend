using ArenaDomain.Entities;
using ArenaDomain.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaInfrastructure.Data.DataSeeding
{
    public static class DataSeeder
    {
      
         public static async Task SeedAsync(
             AppDbContext context,
             UserManager<ApplicationUser> userManager,
             RoleManager<IdentityRole<Guid>> roleManager)
         {
             // Run migrations automatically
             await context.Database.MigrateAsync();

             // ── Seed Roles ────────────────────────────────────────────────
             var roles = new[] { "Admin", "GymMember" };
             foreach (var role in roles)
             {
                 if (!await roleManager.RoleExistsAsync(role))
                     await roleManager.CreateAsync(new IdentityRole<Guid>(role));
             }

             // ── Seed Admin ────────────────────────────────────────────────
             var adminEmail = "admin@arena.com";
             if (await userManager.FindByEmailAsync(adminEmail) is null)
             {
                 var admin = new ApplicationUser
                 {
                     FirstName = "Arena",
                     LastName = "Admin",
                     Email = adminEmail,
                     UserName = adminEmail,
                     PreferredLanguage = "en",
                     IsActive = true,
                     EmailConfirmed = true
                 };

                 await userManager.CreateAsync(admin, "Admin@123456");
                 await userManager.AddToRoleAsync(admin, "Admin");
             }

             // ── Seed Test Member ──────────────────────────────────────────
             var memberEmail = "member@arena.com";
             if (await userManager.FindByEmailAsync(memberEmail) is null)
             {
                 var member = new ApplicationUser
                 {
                     FirstName = "Test",
                     LastName = "Member",
                     Email = memberEmail,
                     UserName = memberEmail,
                     PreferredLanguage = "en",
                     IsActive = true,
                     EmailConfirmed = true
                 };

                 var result = await userManager.CreateAsync(member, "Member@123456");
                 if (result.Succeeded)
                 {
                     await userManager.AddToRoleAsync(member, "GymMember");

                     // Create MemberProfile for test member
                     var memberProfile = new MemberProfile
                     {
                         UserId = member.Id,
                         DateOfBirth = new DateTime(1995, 6, 15),
                         Gender = ArenaDomain.Enums.Gender.Male,
                         Weight = 75,
                         Height = 175
                     };

                     await context.MemberProfiles.AddAsync(memberProfile);
                     await context.SaveChangesAsync();
                 }
             }
           }
      }
}
