namespace SWD_Group4.BusinessLogic.IServices;

public interface INotificationService
{
    Task sendVerifyNotification(int userId, bool isSuccessful, string? reason);

    Task notifySellerSuspended(int userId, string reason, DateTime? suspensionEndAt);

    Task notifySellerUnsuspended(int userId);
}
