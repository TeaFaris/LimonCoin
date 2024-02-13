using LimonCoin.Configuration;
using LimonCoin.Data;
using LimonCoin.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Sockets;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LimonCoin.TelegramBot
{
    public sealed class TelegramBotService
    {
        public TelegramBotClient Client => client;

        readonly TelegramBotClient client;
        readonly ILogger<TelegramBotService> logger;
        readonly IOptions<TelegramSettings> telegramConfig;
        readonly IServiceScopeFactory serviceScopeFactory;
        readonly IWebHostEnvironment env;

        public TelegramBotService(
                TelegramBotClient client,
                ILogger<TelegramBotService> logger,
                IOptions<TelegramSettings> telegramConfig,
                IServiceScopeFactory serviceScopeFactory,
                IWebHostEnvironment env
            )
        {
            this.env = env;
            this.serviceScopeFactory = serviceScopeFactory;
            this.telegramConfig = telegramConfig;
            this.logger = logger;
            this.client = client;

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = []
            };

            client.StartReceiving(
                updateHandler: (botClient, update, cancellationToken) => _ = HandleUpdateAsync(botClient, update, cancellationToken),
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions
            );

            logger.LogInformation("Telegram bot started up successfully");
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message is null && update.CallbackQuery is null)
                {
                    return;
                }

                var message = update.Message;
                var callback = update.CallbackQuery;

                using var scope = serviceScopeFactory.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

                if (message is not null && !string.IsNullOrWhiteSpace(message.Text))
                {
                    var chatId = message.Chat.Id;

                    if (message.Text.StartsWith("/start"))
                    {
                        var commandArgs = message.Text.Trim().Split(' ');

                        var user = dbContext.Users.FirstOrDefault(x => x.TelegramId == chatId);

                        if (user is null)
                        {
                            string avatarRelativePath = $"/avatars/{chatId}.jpg";
                            string avatarAbsolutePath = env.WebRootPath + avatarRelativePath;

                            if (!System.IO.File.Exists(avatarAbsolutePath))
                            {
                                var avatars = await client.GetUserProfilePhotosAsync(chatId, cancellationToken: cancellationToken);

                                try
                                {
                                    var avatarFile = await client.GetFileAsync(avatars.Photos[0][0].FileId, cancellationToken: cancellationToken);

                                    using var fs = System.IO.File.Create(avatarAbsolutePath);

                                    await client.DownloadFileAsync(avatarFile.FilePath!, fs, cancellationToken: cancellationToken);
                                }
                                catch
                                {
                                    System.IO.File.Copy($"{env.WebRootPath}/avatars/placeholder.jpg", avatarAbsolutePath, true);
                                }
                            }

                            long? referrerId = commandArgs.Length > 1 ? long.Parse(commandArgs[1]) : null;

                            ulong initialCoins = 0;

                            var referrer = dbContext
                                    .Users
                                    .FirstOrDefault(x => x.TelegramId == referrerId);

                            if (referrerId is not null)
                            {
                                if(referrer is null)
                                {
                                    referrerId = null;
                                }
                                else
                                {
                                    referrer.Coins += 2500;
                                    referrer.CoinsThisDay += 2500;
                                    referrer.CoinsThisWeek += 2500;

                                    initialCoins = 2500;

                                    var referrals = dbContext.Users.Where(x => x.ReferrerId == referrer.Id);

                                    if (!referrer.CompletedTasks.Contains(LimonTask.Invite10Friends.Id) && referrals.Count() == 9)
                                    {
                                        referrer.CompletedTasks.Add(LimonTask.Invite10Friends.Id);
                                    }

                                    if (!referrer.CompletedTasks.Contains(LimonTask.Invite100Friends.Id) && referrals.Count() == 99)
                                    {
                                        referrer.CompletedTasks.Add(LimonTask.Invite100Friends.Id);
                                    }

                                    dbContext.Users.Update(referrer);
                                }
                            }

                            var newUser = new ApplicationUser
                            {
                                LastTimeClicked = DateTime.UtcNow,
                                TelegramId = chatId,
                                EnergyPerSecond = 1,
                                CoinsPerClick = 3,
                                MaxEnergy = 3000,
                                Energy = 3000,
                                Coins = initialCoins,
                                ReferrerId = referrer?.Id,
                                AwardedTasks = [],
                                CompletedTasks = [],
                                CoinsThisDay = initialCoins,
                                CoinsThisWeek = initialCoins
                            };

                            dbContext.Users.Add(newUser);

                            await dbContext.SaveChangesAsync(cancellationToken);
                        }

                        var keyboard = new InlineKeyboardMarkup(
                        [
                            new InlineKeyboardButton("🕹️ Играть")
                            {
                                WebApp = new WebAppInfo
                                {
                                    Url = telegramConfig.Value.WebAppUrl
                                }
                            },
                            new InlineKeyboardButton("🎓 Как играть")
                            {
                                CallbackData = "info"
                            }
                        ]);

                        await botClient.SendPhotoAsync(
                                chatId: chatId,
                                new InputFileUrl("https://i.imgur.com/xEY7Nqq.png"),
                                caption: $"""
                                          Привет, @{message.Chat.Username} ! Это Лимон 👋 
                                          
                                          Жми на монету и смотри как растет твой баланс.
                                          
                                          🍋 100.000 Лимон - 1$
                                          🍋 1.000.000 Лимон - 10$ 
                                          
                                          Минимальный вывод 🍋 3.000.000 Лимон (30$) на твой криптовалютный кошелек usdt.
                                          
                                          Друзья есть? Зови их в игру.
                                          Так вы вместе получите еще больше монет.
                                          
                                          Лимон - мечты сбываются.

                                          Подпишитесь на наше сообщество:
                                          @limon_coin
                                          """,
                                cancellationToken: cancellationToken,
                                replyMarkup: keyboard
                            );
                    }
                }
                else if (callback is not null && !string.IsNullOrWhiteSpace(callback.Data))
                {
                    var chatId = callback.From.Id;

                    var commandArgs = callback.Data.Trim().Split(' ');

                    switch (commandArgs[0])
                    {
                        case "info":
                            {
                                var keyboard = new InlineKeyboardMarkup(
                                    [
                                        new InlineKeyboardButton("🕹️ Играть")
                                        {
                                            WebApp = new WebAppInfo
                                            {
                                                Url = telegramConfig.Value.WebAppUrl
                                            }
                                        }
                                    ]);

                                await botClient.SendPhotoAsync(
                                        chatId: chatId,
                                        new InputFileUrl("https://i.imgur.com/c3o3MjU_d.webp?maxwidth=760&fidelity=grand"),
                                        caption: $"""
                                          Как играть в Лимон 🍋
                                          
                                          Tap to earn
                                          Лимон - это виртуальная игра-кликер, в которой вы зарабатываете монеты, нажимая на экран. 
                                          
                                          Withdrawal
                                          Монеты можно вывести криптовалютой USDT.
                                          
                                          Boosts
                                          Получай бусты и выполняй задания, чтобы получить больше монет Лимона.
                                          
                                          Frens
                                          Пригласи кого-нибудь, и вы оба получите бонусы. Помогите другу перейти в следующие лигу, и вы получите ещё больше Лимона.

                                          Подпишитесь на наше сообщество:
                                          @limon_coin
                                          """,
                                        cancellationToken: cancellationToken,
                                        replyMarkup: keyboard
                                    );
                                break;
                            }
                        case "confirm":
                            {
                                var userId = long.Parse(commandArgs[1]);
                                var boost = int.Parse(commandArgs[2]);

                                var user = dbContext.Users.FirstOrDefault(x => x.TelegramId == userId);

                                switch (boost)
                                {
                                    case 0:
                                        {
                                            user.ClickerLevel += 1;
                                            user.CoinsPerClick += 2;
                                            break;
                                        }
                                    case 1:
                                        {
                                            user.EnergyRecoveryLevel += 1;
                                            user.EnergyPerSecond += 2;
                                            break;
                                        }
                                    case 2:
                                        {
                                            user.EnergyCapacityLevel += 1;
                                            user.MaxEnergy += 7000;
                                            break;
                                        }
                                }

                                if (!user.CompletedTasks.Contains(LimonTask.BuyBoost.Id))
                                {
                                    user.CompletedTasks.Add(LimonTask.BuyBoost.Id);
                                }

                                dbContext.Users.Update(user);

                                await dbContext.SaveChangesAsync(cancellationToken);

                                await botClient.SendTextMessageAsync(chatId, "Буст выдан!", cancellationToken: cancellationToken);
                                break;
                            }
                        case "requestconfirmation":
                            {
                                var boost = int.Parse(commandArgs[1]);

                                var user = dbContext.Users
                                    .FirstOrDefault(x => x.TelegramId == chatId);

                                int price = boost switch
                                {
                                    0 => user.ClickerLevel == 2 ? 1499 : 2499,
                                    1 => user.EnergyRecoveryLevel == 2 ? 1499 : 2499,
                                    2 => user.EnergyCapacityLevel == 2 ? 1499 : 2499
                                };

                                var keyboard = new InlineKeyboardMarkup(
                                            [
                                                new InlineKeyboardButton("Подтвердить")
                                                {
                                                    CallbackData = $"confirm {chatId} {boost}"
                                                }
                                            ]);

                                var tgUser = await botClient.GetChatAsync(chatId, cancellationToken: cancellationToken);
                                await botClient.SendTextMessageAsync(6940830288, $"""
                                                                                  Поступил новый запрос на подтверждение покупки({price} р.) от @{tgUser.Username ?? tgUser.FirstName} (Telegram Id: {tgUser.Id})

                                                                                  Если деньги не пришли, то просто проигнорируйте это сообщение.
                                                                                  """,
                                                                                  replyMarkup: keyboard,
                                                                                  cancellationToken: cancellationToken);

                                await botClient.SendTextMessageAsync(chatId, "Проверка оплаты может занять до 3-х часов.", cancellationToken: cancellationToken);

                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected Telegram API error");
            }
        }

        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            logger.LogError(exception, "Unexpected Telegram API error");

            return Task.CompletedTask;
        }
    }
}
