using LimonCoin.Shared;

namespace LimonCoin.Hubs
{
    public interface IClickerHub
    {
        public Task ReceiveUpdate(UserDTO telegramId);

        public Task ReceivedNotification(string notificationMessage);
    }
}
