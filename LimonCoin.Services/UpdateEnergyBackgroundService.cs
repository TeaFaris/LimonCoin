using LimonCoin.Data;
using LimonCoin.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

namespace LimonCoin.Services
{
    public class UpdateEnergyBackgroundService(IServiceScopeFactory serviceScopeFactory, TelegramBotClient tgClient, IHubContext<ClickerHub, IClickerHub> clickerHub) : BackgroundService
    {
        static readonly TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

        private DateTime lastUpdateTime = DateTime.Now;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = serviceScopeFactory.CreateScope();

                using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

                var parallelTask = Parallel.ForEachAsync(dbContext.Users.Where(x => x.Energy < x.MaxEnergy), async (user, ct) =>
                {
                    user.Energy++;
                    
                    await clickerHub.Clients.Group(user.TelegramId.ToString()).ReceiveUpdate(new()
                    {
                        TelegramId = user.TelegramId,
                        Coins = user.Coins,
                        CoinsPerClick = user.CoinsPerClick,
                        Energy = user.Energy,
                        EnergyPerSecond = user.EnergyPerSecond,
                        LastTimeClicked = user.LastTimeClicked,
                        MaxEnergy = user.MaxEnergy
                    });
                }).ContinueWith(task => dbContext.SaveChangesAsync(stoppingToken), stoppingToken);

                await Task.WhenAll(Task.Delay(UpdateRate, stoppingToken), parallelTask);

                if (lastUpdateTime.Day != DateTime.Now.Day)
                {
                    await Parallel.ForEachAsync(dbContext.Users, async (user, ct) =>
                    {
                        user.CoinsThisDay = 0;
                    });
                }
                if (lastUpdateTime.DayOfWeek == DayOfWeek.Sunday && DateTime.Now.DayOfWeek == DayOfWeek.Monday)
                {
                    await Parallel.ForEachAsync(dbContext.Users, async (user, ct) =>
                    {
                        user.CoinsThisWeek = 0;
                    });
                }

                lastUpdateTime = DateTime.Now;
            }
        }
    }
}
