namespace ArenaApplication.IServices
{
    public interface IGoogleTokenValidator
    {
        Task<GoogleUserInfo?> ValidateAsync(string idToken);
    }

    public class GoogleUserInfo
    {
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
    }
}