using LimonCoin.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace LimonCoin.TelegramBot
{
    public static class TelegramBotExtensions
    {
        public static IServiceCollection AddTelegramBot(this IServiceCollection services)
        {
            using var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetService<IOptions<TelegramSettings>>()!;

            var botClient = new TelegramBotClient(config.Value.Token);

            services.AddSingleton(botClient);
            services.AddSingleton<TelegramBotService>();

            return services;
        }
    }
}
