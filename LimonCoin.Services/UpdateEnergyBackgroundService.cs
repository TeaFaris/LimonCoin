using LimonCoin.Data;
using LimonCoin.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace LimonCoin.Services
{
    public class UpdateEnergyBackgroundService(IServiceScopeFactory serviceScopeFactory, TelegramBotClient tgClient, IHubContext<ClickerHub, IClickerHub> clickerHub, ILogger<UpdateEnergyBackgroundService> logger) : BackgroundService
    {
        static readonly TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

        private const int SecondsPerEnergy = 3;

        private int currentState = 1;

        private DateTime lastUpdateTime = DateTime.Now;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = serviceScopeFactory.CreateScope();

                    using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

                    var parallelTask = Parallel.ForEachAsync(dbContext.Users.Where(x => x.Energy < x.MaxEnergy), async (user, ct) =>
                    {
                        if (user.LastTimeClicked.AddSeconds(1) >= DateTime.UtcNow)
                        {
                            return;
                        }

                        if (currentState == SecondsPerEnergy)
                        {
                            user.Energy += user.EnergyPerSecond;
                        }

                        await clickerHub.Clients.Group(user.TelegramId.ToString()).ReceiveUpdate(new()
                        {
                            TelegramId = user.TelegramId,
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
                    }).ContinueWith(task => dbContext.SaveChangesAsync(stoppingToken), stoppingToken);

                    await Task.WhenAll(Task.Delay(UpdateRate, stoppingToken), parallelTask);

                    if (lastUpdateTime.Day != DateTime.UtcNow.Day)
                    {
                        await Parallel.ForEachAsync(dbContext.Users, async (user, ct) =>
                        {
                            user.CoinsThisDay = 0;
                        });
                    }
                    if (lastUpdateTime.DayOfWeek == DayOfWeek.Sunday && DateTime.UtcNow.DayOfWeek == DayOfWeek.Monday)
                    {
                        await Parallel.ForEachAsync(dbContext.Users, async (user, ct) =>
                        {
                            user.CoinsThisWeek = 0;
                        });
                    }

                    lastUpdateTime = DateTime.Now;

                    currentState++;
                    if (currentState > SecondsPerEnergy)
                    {
                        currentState = 1;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Unexpected error, skipping the tick");
                }
            }
        }
    }
}
