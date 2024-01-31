namespace LimonCoin.Models
{
    public sealed class LimonTask
    {
        public static LimonTask SubscribeOrGoto1 = new()
        {
            Id = Guid.Parse("aebcc867-95f2-4cf5-93a5-702635088323"),
            Reward = 50000,
            TelegramChannelId = -1002141536305,
            Name = "Вступить в сообщество Limon",
            ImagePathUrl = "images/home/coin.png"
        };

        public static LimonTask SubscribeOrGoto2 = new()
        {
            Id = Guid.Parse("650ddd09-23bd-45bd-b884-1e90548d8fae"),
            Reward = 50000,
            Link = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Name = "Посмотреть видео",
            ImagePathUrl = "images/home/coin.png",
            Disabled = true
        };

        public static readonly LimonTask BuyBoost = new()
        {
            Id = Guid.Parse("cbc193e9-0281-49cb-9428-ffdea0eded71"),
            Reward = 300000,
            Name = "Купить один из бустов",
            ImagePathUrl = "images/emojis/rocket.png"
        };

        public static readonly LimonTask Invite10Friends = new()
        {
            Id = Guid.Parse("724f13a1-2f0a-454b-bc66-f0457a11f2a3"),
            Reward = 100000,
            Name = "Пригласить 10 друзей",
            ImagePathUrl = "images/emojis/bear.png"
        };
        
        public static readonly LimonTask Invite100Friends = new()
        {
            Id = Guid.Parse("82c423f6-cc39-48d3-b4d9-36762432d90e"),
            Reward = 500000,
            Name = "Пригласить 100 друзей",
            ImagePathUrl = "images/emojis/bear.png"
        };

        public static readonly LimonTask AllTasks = new()
        {
            Id = Guid.Parse("f78543de-903b-4a47-b4e5-b31580490b61"),
            Reward = 1000000,
            Name = "Выполнить все задания",
            ImagePathUrl = "images/cups/gold.png"
        };

        public static List<LimonTask> Tasks
        {
            get
            {
                List<LimonTask> tasks = [SubscribeOrGoto1, SubscribeOrGoto2, BuyBoost, Invite10Friends, Invite100Friends, AllTasks];
                tasks.RemoveAll(x => x.Disabled);

                return tasks;
            }
        }

        public Guid Id { get; init; }

        public ulong Reward { get; init; }

        public string? Link { get; set; }

        public long? TelegramChannelId { get; init; }

        public string Name { get; init; }

        public string ImagePathUrl { get; init; }

        public bool Disabled { get; init; }
    }
}
