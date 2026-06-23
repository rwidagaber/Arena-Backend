using ArenaDomain.Entities.User;
using Microsoft.AspNetCore.Identity;

namespace ArenaMVC.Services;

public class MvcAdminLoginService : IMvcAdminLoginService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public MvcAdminLoginService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<AdminLoginResult?> ValidateAdminAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return null;

        if (!user.IsActive)
            return null;

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        // Only allow Admin role to sign in through this portal
        if (!roles.Contains("Admin"))
            return null;

        return new AdminLoginResult(
            UserId: user.Id.ToString(),
            Email: user.Email!,
            FullName: $"{user.FirstName} {user.LastName}".Trim(),
            Role: role
        );
    }
}
