using LimonCoin.Configuration;
using LimonCoin.Data;
using LimonCoin.Hubs;
using LimonCoin.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LimonCoin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class UserController(ApplicationDBContext dbContext, TelegramBotClient tgClient, IOptions<TelegramSettings> telegramConfig) : ControllerBase
    {
        [HttpPost("dawdshiuhawd/{id}")]
        public async Task<ActionResult> AddCoins(long id, [FromQuery] long coins)
        {
            var user = dbContext.Users
                .FirstOrDefault(x => x.TelegramId == id);

            if (user is null)
            {
                return NotFound();
            }

            user.Coins += (ulong)coins;

            dbContext.Update(user);
            dbContext.SaveChanges();

            return Ok();
        }

        [HttpPost("requestPayment")]
        public async Task<ActionResult> RequestPayment([FromQuery] long id, [FromQuery] int boost)
        {
            var user = dbContext.Users
                .FirstOrDefault(x => x.TelegramId == id);

            if (user is null)
            {
                return NotFound();
            }

            if (boost < 0 || boost > 2)
            {
                return NotFound();
            }

            int price = boost switch
            {
                0 => user.ClickerLevel == 2 ? 1499 : 2499,
                1 => user.EnergyRecoveryLevel == 2 ? 1499 : 2499,
                2 => user.EnergyCapacityLevel == 2 ? 1499 : 2499
            };

            var keyboard = new InlineKeyboardMarkup(
                                            [
                                                new InlineKeyboardButton("Подтвердить оплату")
                                                {
                                                    CallbackData = $"requestconfirmation {boost}"
                                                }
                                            ]);

            var tgUser = await tgClient.GetChatAsync(id);
            await tgClient.SendTextMessageAsync(
                    id,
                    $"""
                     Чтобы получить реквизиты для оплаты напишите в тех. поддержку
                     @limon_coin_ads
                     """,
                    replyMarkup: keyboard
                );

            return Ok();
        }

        [HttpGet("search/{username}")]
        public async Task<ActionResult<List<UserDTO>>> SearchUsers(string username)
        {
            var userDTOs = new List<UserDTO>();

            await Parallel.ForEachAsync(
                    dbContext.Users,
                    async (user, ct) =>
                    {
                        var userChat = await tgClient.GetChatAsync(user.TelegramId, ct);

                        var name = userChat.Username ?? userChat.FirstName;

                        if (!name.Contains(username, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return;
                        }

                        userDTOs.Add(new UserDTO
                        {
                            TelegramId = user.TelegramId,
                            Coins = user.Coins,
                            CoinsPerClick = user.CoinsPerClick,
                            Energy = user.Energy,
                            EnergyPerSecond = user.EnergyPerSecond,
                            LastTimeClicked = user.LastTimeClicked,
                            MaxEnergy = user.MaxEnergy,
                            Username = name,
                            ReferrerId = null,
                            ReferralTelegramIds = [],
                            CoinsThisWeek = user.CoinsThisWeek,
                            CoinsThisDay = user.CoinsThisDay,
                            AwardedTasks = user.AwardedTasks,
                            CompletedTasks = user.CompletedTasks
                        });
                    }
                );

            return userDTOs;
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsers()
        {
            var userDTOs = new List<UserDTO>();

            await Parallel.ForEachAsync(
                    dbContext.Users.OrderByDescending(x => x.Coins).Take(20),
                    async (user, ct) =>
                    {
                        var userChat = await tgClient.GetChatAsync(user.TelegramId, ct);
                        var username = userChat.Username ?? userChat.FirstName;

                        userDTOs.Add(new UserDTO
                        {
                            TelegramId = user.TelegramId,
                            Coins = user.Coins,
                            CoinsPerClick = user.CoinsPerClick,
                            Energy = user.Energy,
                            EnergyPerSecond = user.EnergyPerSecond,
                            LastTimeClicked = user.LastTimeClicked,
                            MaxEnergy = user.MaxEnergy,
                            Username = username,
                            ReferrerId = null,
                            ReferralTelegramIds = [],
                            CoinsThisWeek = user.CoinsThisWeek,
                            CoinsThisDay = user.CoinsThisDay,
                            AwardedTasks = user.AwardedTasks,
                            CompletedTasks = user.CompletedTasks
                        });
                    }
                );

            return new(userDTOs.OrderByDescending(x => x.Coins));
        }

        [HttpPost("withdraw/{id}")]
        public async Task<ActionResult<bool>> Withdraw(long id, [FromQuery] string wallet, [FromQuery] long coins)
        {
            var user = dbContext.Users
                .FirstOrDefault(x => x.TelegramId == id);

            if (user is null)
            {
                return NotFound();
            }

            if (user.Coins < (ulong)coins || 3_000_000 >= coins)
            {
                return false;
            }

            user.Coins -= (ulong)coins;

            var tgUser = await tgClient.GetChatAsync(id);
            await tgClient.SendTextMessageAsync(
                    6940830288,
                    $"""
                    Поступил новый запрос на вывод {coins:#,0} ({coins / 100_000m}$) монет от @{tgUser.Username ?? tgUser.FirstName} (TelegramId: {tgUser.Id})

                    Кошелёк: {wallet}
                    """
                );

            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync();

            return true;
        }

        [HttpGet("count")]
        public async Task<ActionResult<long>> GetCount()
        {
            return await dbContext.Users.LongCountAsync();
        }

        [HttpGet("coinsCount")]
        public async Task<ActionResult<decimal>> GetCoinsCount()
        {
            return await dbContext.Users.SumAsync(x => (decimal)x.Coins);
        }

        [HttpGet("onlineCount")]
        public async Task<ActionResult<long>> GetOnlineCount()
        {
            return ClickerHub.ConnectedUsers.GroupBy(x => x.Value).Count();
        }

        [HttpGet("dailyCount")]
        public async Task<ActionResult<long>> GetDailyCount()
        {
            return await dbContext.Users.Where(x => x.CoinsThisDay > 0).LongCountAsync();
        }

        [HttpGet("top/day")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetDayTop([FromQuery] LeagueType leagueType)
        {
            var userDTOs = new List<UserDTO>();

            var min = leagueType.GetMinCoinsForLeague();
            var max = leagueType.GetMaxCoinsForLeague();

            await Parallel.ForEachAsync(
                    dbContext
                        .Users
                        .Where(x => x.Coins >= min && x.Coins < max)
                        .OrderByDescending(x => x.CoinsThisDay)
                        .Take(100),
                    async (user, ct) =>
                    {
                        var userChat = await tgClient.GetChatAsync(user.TelegramId, ct);
                        var username = userChat.Username ?? userChat.FirstName;

                        userDTOs.Add(new UserDTO
                        {
                            TelegramId = user.TelegramId,
                            Coins = user.Coins,
                            CoinsPerClick = user.CoinsPerClick,
                            Energy = user.Energy,
                            EnergyPerSecond = user.EnergyPerSecond,
                            LastTimeClicked = user.LastTimeClicked,
                            MaxEnergy = user.MaxEnergy,
                            Username = username,
                            ReferrerId = null,
                            ReferralTelegramIds = [],
                            CoinsThisWeek = user.CoinsThisWeek,
                            CoinsThisDay = user.CoinsThisDay,
                            AwardedTasks = user.AwardedTasks,
                            CompletedTasks = user.CompletedTasks
                        });
                    }
                );

            return new(userDTOs.OrderByDescending(x => x.CoinsThisDay));
        }

        [HttpGet("top/week")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetDayWeek([FromQuery] LeagueType leagueType)
        {
            var userDTOs = new List<UserDTO>();

            var min = leagueType.GetMinCoinsForLeague();
            var max = leagueType.GetMaxCoinsForLeague();

            await Parallel.ForEachAsync(
                    dbContext
                        .Users
                        .Where(x => x.Coins >= min && x.Coins < max)
                        .OrderByDescending(x => x.CoinsThisWeek)
                        .Take(100),
                    async (user, ct) =>
                        {
                            var userChat = await tgClient.GetChatAsync(user.TelegramId, ct);
                            var username = userChat.Username ?? userChat.FirstName;

                            userDTOs.Add(new UserDTO
                            {
                                TelegramId = user.TelegramId,
                                Coins = user.Coins,
                                CoinsPerClick = user.CoinsPerClick,
                                Energy = user.Energy,
                                EnergyPerSecond = user.EnergyPerSecond,
                                LastTimeClicked = user.LastTimeClicked,
                                MaxEnergy = user.MaxEnergy,
                                Username = username,
                                ReferrerId = null,
                                ReferralTelegramIds = [],
                                CoinsThisWeek = user.CoinsThisWeek,
                                CoinsThisDay = user.CoinsThisDay,
                                AwardedTasks = user.AwardedTasks,
                                CompletedTasks = user.CompletedTasks,
                            });
                        }
                );

            return new(userDTOs.OrderByDescending(x => x.CoinsThisDay));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDTO>> Get(long id)
        {
            var user = dbContext.Users
                .Include(x => x.Referrer)
                .FirstOrDefault(x => x.TelegramId == id);
            
            if(user is null)
            {
                return NotFound();
            }

            var userChat = await tgClient.GetChatAsync(user.TelegramId);
            var username = userChat.Username ?? userChat.FirstName;

            var referrals = dbContext
                                .Users
                                .Include(x => x.Referrer)
                                .Where(x => x.ReferrerId == user.Id)
                                .Select(x => x.TelegramId);

            return new UserDTO
            {
                TelegramId = user.TelegramId,
                Coins = user.Coins,
                CoinsPerClick = user.CoinsPerClick,
                Energy = user.Energy,
                EnergyPerSecond = user.EnergyPerSecond,
                LastTimeClicked = user.LastTimeClicked,
                MaxEnergy = user.MaxEnergy,
                Username = username,
                ReferrerId = user?.Referrer?.TelegramId,
                ReferralTelegramIds = referrals,
                CoinsThisWeek = user.CoinsThisWeek,
                CoinsThisDay = user.CoinsThisDay,
                AwardedTasks = user.AwardedTasks,
                CompletedTasks = user.CompletedTasks,
                ClickerLevel = user.ClickerLevel,
                EnergyCapacityLevel = user.EnergyCapacityLevel,
                EnergyRecoveryLevel = user.EnergyRecoveryLevel
            };
        }

        [HttpPost("sendReferralLink/{id}")]
        public async Task<ActionResult> SendReferralLink(long id)
        {
            var user = dbContext
                .Users
                .FirstOrDefault(x => x.TelegramId == id);

            if (user is null)
            {
                return NotFound();
            }
           
            var botself = await tgClient.GetMeAsync();

            var keyboard = new InlineKeyboardMarkup(
                        [
                            new InlineKeyboardButton("Пригласить друга")
                            {
                                Url = $"https://t.me/share/url?text=2.500%20Limoncoin%20в%20качестве%20первого%20подарка&url=https://t.me/{botself.Username}?start={id}"
                            }
                        ]);

            await tgClient.SendTextMessageAsync(new ChatId(id), $"""
                                                                Приглашайте друзей и получайте бонусы за каждого приглашенного друга! 🎁

                                                                Ваша реферальная ссылка: https://t.me/{botself.Username}?start={id}
                                                                """,
                                                                replyMarkup: keyboard);

            return Ok();
        }
    }
}
