using LimonCoin.Data;
using LimonCoin.Hubs;
using LimonCoin.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Net.Http.Headers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LimonCoin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class UserController(ApplicationDBContext dbContext, TelegramBotClient tgClient) : ControllerBase
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
        public async Task<ActionResult<List<UserDTO>>> GetUsers([FromQuery] uint offset)
        {
            var userDTOs = new List<UserDTO>();

            await Parallel.ForEachAsync(
                    dbContext
                        .Users
                        .Skip((int)offset)
                        .Take(20),
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

            return userDTOs;
        }

        [HttpPost("withdraw/{id}")]
        public async Task<ActionResult<bool>> Withdraw(long id, [FromQuery] string wallet)
        {
            var user = dbContext.Users
                .FirstOrDefault(x => x.TelegramId == id);

            if (user is null)
            {
                return NotFound();
            }

            if (user.Coins < 10_000_000)
            {
                return false;
            }

            user.Coins -= 10_000_000;

            var tgUser = await tgClient.GetChatAsync(id);
            await tgClient.SendTextMessageAsync(
                    439217086,
                    $"""
                    Поступил новый запрос на вывод 10,000,000 (100$) монет от @{tgUser.Username ?? tgUser.FirstName}

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
            return ClickerHub.ConnectedUsers.Count;
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
                                CompletedTasks = user.CompletedTasks
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
                ReferrerId = null,
                ReferralTelegramIds = [],
                CoinsThisWeek = user.CoinsThisWeek,
                CoinsThisDay = user.CoinsThisDay,
                AwardedTasks = user.AwardedTasks,
                CompletedTasks = user.CompletedTasks
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
