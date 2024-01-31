using LimonCoin.Data;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace LimonCoin.Hubs
{
    public sealed class ClickerHub(ApplicationDBContext dbContext) : Hub<IClickerHub>
    {
        public static ConcurrentDictionary<string, long> ConnectedUsers = new();

        public async Task Register(long telegramId)
        {
            var user = dbContext.Users.FirstOrDefault(x => x.TelegramId == telegramId);

            if (user is null)
            {
                await Clients.Caller.ReceivedNotification("Пропишите боту заново /start");
                Context.Abort();
                return;
            }

            ConnectedUsers.TryAdd(Context.ConnectionId, telegramId);
            await Groups.AddToGroupAsync(Context.ConnectionId, telegramId.ToString());

            if(user.CoinsThisDay == 0)
            {
                user.Coins += 10000;
                user.CoinsThisDay += 10000;
                user.CoinsThisWeek += 10000;

                dbContext.Users.Update(user);
                await dbContext.SaveChangesAsync();

                await Clients.Caller.ReceivedNotification("Вы получили ежедневный бонус 10.000 монет");
            }

            await Clients.Caller.ReceiveUpdate(new()
            {
                Coins = user.Coins,
                CoinsPerClick = user.CoinsPerClick,
                Energy = user.Energy,
                EnergyPerSecond = user.EnergyPerSecond,
                LastTimeClicked = user.LastTimeClicked,
                MaxEnergy = user.MaxEnergy,
                ClickerLevel = user.ClickerLevel,
                EnergyCapacityLevel = user.EnergyCapacityLevel,
                EnergyRecoveryLevel = user.EnergyRecoveryLevel
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            ConnectedUsers.TryRemove(Context.ConnectionId, out var _);
        }

        public async Task Click(long telegramId)
        {
            var user = dbContext.Users.FirstOrDefault(x => x.TelegramId == telegramId);

            if(user is null)
            {
                return;
            }

            if (DateTime.UtcNow < user.LastTimeClicked.AddMilliseconds(20))
            {
                return;
            }

            if (user.Energy < user.CoinsPerClick)
            {
                return;
            }

            user.Coins += user.CoinsPerClick;
            user.CoinsThisDay += user.CoinsPerClick;
            user.CoinsThisWeek += user.CoinsPerClick;

            user.Energy -= user.CoinsPerClick;

            user.LastTimeClicked = DateTime.UtcNow;

            dbContext.Update(user);

            await dbContext.SaveChangesAsync();

            await Clients.Caller.ReceiveUpdate(new()
            {
                Coins = user.Coins,
                CoinsPerClick = user.CoinsPerClick,
                Energy = user.Energy,
                EnergyPerSecond = user.EnergyPerSecond,
                LastTimeClicked = user.LastTimeClicked,
                MaxEnergy = user.MaxEnergy,
                ClickerLevel = user.ClickerLevel,
                EnergyCapacityLevel = user.EnergyCapacityLevel,
                EnergyRecoveryLevel = user.EnergyRecoveryLevel
            });
        }
    }
}
