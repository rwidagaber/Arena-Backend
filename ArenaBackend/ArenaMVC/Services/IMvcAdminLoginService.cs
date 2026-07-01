namespace ArenaMVC.Services;

public interface IMvcAdminLoginService
{
    /// <summary>
    /// Validates admin credentials.
    /// Returns the user's display name on success, or null on failure.
    /// </summary>
    Task<AdminLoginResult?> ValidateAdminAsync(string email, string password);
}

public record AdminLoginResult(string UserId, string Email, string FullName, string Role);
