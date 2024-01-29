using LimonCoin.Data;
using LimonCoin.Models;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;

namespace LimonCoin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class TasksController(ApplicationDBContext dbContext, TelegramBotClient tgClient) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LimonTask>>> GetAll()
        {
            var tasks = LimonTask.Tasks.ToList();

            foreach (var task in tasks)
            {
                if (task.TelegramChannelId is null)
                {
                    continue;
                }

                var channel = tgClient.GetChatAsync(task.TelegramChannelId).Result;

                task.Link = "https://t.me/" + channel.Username;
            }

            return tasks;
        }

        [HttpPost("ct")]
        public async Task<ActionResult> ChangeTask([FromQuery] string password, [FromQuery] int taskId, [FromQuery] string name, [FromQuery] int reward, [FromQuery] long? telegramChannelId, [FromQuery] string? link)
        {
            if(password != "BoMVh6BLFbKeAbs")
            {
                return Unauthorized();
            }

            if(taskId == 0)
            {
                LimonTask.SubscribeOrGoto1 = new()
                {
                    Id = Guid.NewGuid(),
                    ImagePathUrl = "images/home/coin.png",
                    Link = string.IsNullOrWhiteSpace(link) ? null : link,
                    Name = name,
                    Reward = (ulong)reward,
                    TelegramChannelId = telegramChannelId
                };
            }
            else if(taskId == 1)
            {
                LimonTask.SubscribeOrGoto2 = new()
                {
                    Id = Guid.NewGuid(),
                    ImagePathUrl = "images/home/coin.png",
                    Link = string.IsNullOrWhiteSpace(link) ? null : link,
                    Name = name,
                    Reward = (ulong)reward,
                    TelegramChannelId = telegramChannelId
                };
            }

            return Ok();
        }

        [HttpGet("check/{id}")]
        public async Task<ActionResult<bool>> CheckTask(Guid id, [FromQuery] long telegramId)
        {
            var user = dbContext.Users.FirstOrDefault(x => x.TelegramId == telegramId);

            if (user is null)
            {
                return NotFound();
            }

            var task = Array.Find(LimonTask.Tasks, x => x.Id == id);

            if(task is null)
            {
                return NotFound();
            }

            if((!user.CompletedTasks.Contains(LimonTask.SubscribeOrGoto1.Id) && task == LimonTask.SubscribeOrGoto1) ||
               (!user.CompletedTasks.Contains(LimonTask.SubscribeOrGoto2.Id) && task == LimonTask.SubscribeOrGoto2))
            {
                try
                {
                    if(task.Link is not null)
                    {
                        user.CompletedTasks.Add(task.Id);
                    }
                    else if(task.TelegramChannelId is not null)
                    {
                        await tgClient.GetChatMemberAsync(task.TelegramChannelId, telegramId);

                        user.CompletedTasks.Add(task.Id);
                    }

                    dbContext.Users.Update(user);
                    await dbContext.SaveChangesAsync();
                }
                catch
                {
                    // Ignore
                }
            }

            if(user.CompletedTasks.Contains(id) && !user.AwardedTasks.Contains(id))
            {
                user.AwardedTasks.Add(id);

                user.Coins += task.Reward;
                user.CoinsThisDay += task.Reward;
                user.CoinsThisWeek += task.Reward;

                dbContext.Users.Update(user);
                await dbContext.SaveChangesAsync();

                return true;
            }

            return false;
        }
    }
}
