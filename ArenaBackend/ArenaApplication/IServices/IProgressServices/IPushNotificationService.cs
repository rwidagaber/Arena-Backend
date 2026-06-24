// ArenaApplication/IServices/IPushNotificationService.cs
namespace ArenaApplication.IServices
{
    public interface IPushNotificationService
    {
        Task SendAsync(Guid userId, string title, string message, string url = "/dashboard");
    }
}